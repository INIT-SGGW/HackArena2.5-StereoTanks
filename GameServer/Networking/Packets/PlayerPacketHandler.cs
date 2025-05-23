﻿using System.Collections.Concurrent;
using System.Text.Json;
using GameLogic.Networking;
using Serilog;

namespace GameServer;

/// <summary>
/// Handles packets coming from player connections.
/// </summary>
/// <param name="game">The game instance.</param>
/// <param name="logger">The logger instance.</param>
internal sealed class PlayerPacketHandler(
    GameInstance game,
#if HACKATHON
    Action<PlayerConnection>? onHackathonBotMadeAction,
#endif
    ILogger logger)
    : IPacketSubhandler
{
    private readonly PlayerActionHandler actionHandler = new(game, logger);

#if STEREO
    private readonly GoToService goToService = new(game, logger);
#endif

#if HACKATHON

    /// <summary>
    /// Gets the list of actions for hackathon bots.
    /// </summary>
    public ConcurrentDictionary<PlayerConnection, Action> HackathonBotActions { get; } = [];

#endif

    /// <inheritdoc/>
    public bool CanHandle(Connection connection, Packet packet)
    {
        return connection is PlayerConnection;
    }

    /// <inheritdoc/>
    public Task<bool> HandleAsync(Connection connection, Packet packet)
    {
        var player = (PlayerConnection)connection;

        if (!packet.Type.HasFlag(PacketType.PlayerResponseActionGroup))
        {
            return Task.FromResult(false);
        }

        if (packet.Type is PacketType.GameStatusRequest)
        {
            return Task.FromResult(false);
        }

#if HACKATHON

        this.ValidateGameStateForBot(player, packet);

#endif

        if (!this.ValidateTickAndAlive(player, packet))
        {
            return Task.FromResult(true);
        }

        var action = this.GetPlayerAction(player, packet);

        if (action is not null)
        {
            if (player.IsHackathonBot)
            {
                lock (game)
                {
                    this.HackathonBotActions.AddOrUpdate(player, action, (_, _) => action);
                }
            }

            lock (game)
            {
                if (!player.IsHackathonBot)
                {
                    action?.Invoke();
                }

                this.RegisterPlayerAction(player);
            }
        }

        return Task.FromResult(true);
    }

    private bool ValidateTickAndAlive(PlayerConnection player, Packet packet)
    {
        lock (player)
        {
            if (player.HasMadeActionThisTick)
            {
#if HACKATHON && STEREO
                if (player.IsHackathonBot || packet.Type != PacketType.GoTo)
#elif STEREO
                if (packet.Type != PacketType.GoTo)
#endif
                {
                    this.SendWarning(player, PacketType.PlayerAlreadyMadeActionWarning);
                }

                return false;
            }
        }

        if (player.Instance.Tank.IsDead && packet.Type is not PacketType.Pass)
        {
            this.SendWarning(player, PacketType.ActionIgnoredDueToDeadWarning);
            return false;
        }

        return true;
    }

    private Action? GetPlayerAction(PlayerConnection player, Packet packet)
    {
        Action? action = null;
        IPacketPayload? responsePayload = null;

        switch (packet.Type)
        {
            case PacketType.Movement:
                var move = packet.GetPayload<MovementPayload>();
                action = () => this.actionHandler.HandleMovement(player, move.Direction);
                break;

            case PacketType.Rotation:
                var rot = packet.GetPayload<RotationPayload>();
                action = () => this.actionHandler.HandleRotation(player, rot.TankRotation, rot.TurretRotation);
                break;

            case PacketType.AbilityUse:
                var ability = packet.GetPayload<AbilityUsePayload>();
                action = () => this.actionHandler.GetAbilityAction(ability.AbilityType, player.Instance)?.Invoke();
                break;

#if STEREO

            case PacketType.CaptureZone:
                action = () => this.actionHandler.HandleZoneCapture(player, out responsePayload);
                break;

            case PacketType.GoTo:
                lock (game)
                {
                    if (this.goToService.TryResolve(player, packet, out var ctx, out responsePayload))
                    {
                        action = () => this.goToService.Execute(ctx!);
                    }
                }

                break;

#endif
        }

        if (responsePayload is not null)
        {
            var responsePacket = new ResponsePacket(responsePayload, logger);
            _ = responsePacket.SendAsync(player);
        }

        return action;
    }

    private void RegisterPlayerAction(PlayerConnection player)
    {
        lock (player)
        {
            player.HasMadeActionThisTick = true;
#if HACKATHON
            onHackathonBotMadeAction?.Invoke(player);
#endif
        }
    }

#if HACKATHON

    private void ValidateGameStateForBot(PlayerConnection player, Packet packet)
    {
        if (!player.IsHackathonBot)
        {
            return;
        }

        var gameStateIdProperty = JsonNamingPolicy.CamelCase.ConvertName(nameof(ActionPayload.GameStateId));
        var receivedId = (string?)packet.Payload[gameStateIdProperty];

        lock (game.GameManager.CurrentGameStateId!)
        {
            var isCurrent = receivedId is not null && game.GameManager.CurrentGameStateId == receivedId;

            if (receivedId is null)
            {
                this.SendWarning(player, "GameStateId is missing in the payload");
            }

            if (!isCurrent)
            {
                this.SendWarning(player, PacketType.SlowResponseWarning);
            }

            player.HasMadeActionToCurrentGameState = true;
        }
    }

#endif

    private void SendWarning(PlayerConnection player, string message)
    {
        logger.Verbose(message + " ({player})", player);
        var payload = new CustomWarningPayload(message);
        _ = new ResponsePacket(payload, logger).SendAsync(player);
    }

    private void SendWarning(PlayerConnection player, PacketType type)
    {
        var payload = new EmptyPayload { Type = type };
        _ = new ResponsePacket(payload, logger).SendAsync(player);
    }
}
