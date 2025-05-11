﻿using System.Net;
using System.Net.WebSockets;
using Serilog;

namespace GameServer;

/// <summary>
/// Represents a connection.
/// </summary>
/// <param name="Context">The HTTP listener context.</param>
/// <param name="Socket">The WebSocket.</param>
/// <param name="Data">The connection data.</param>
/// <param name="Logger">The logger.</param>
internal abstract record class Connection(
    HttpListenerContext Context,
    WebSocket Socket,
    ConnectionData Data,
    ILogger Logger)
{
    private readonly object pingSentLock = new();
    private readonly object hasSentPongLock = new();
    private readonly object readyLock = new();

    private bool isReadyToReceiveGameState;
    private DateTime lastPingSendTime;
    private bool isSecondPingAttempt;
    private bool hasSentPong;

    /// <summary>
    /// Gets the SemaphoreSlim for sending packets.
    /// </summary>
    public SemaphoreSlim SendPacketSemaphore { get; } = new(1, 1);

    /// <summary>
    /// Gets the ip of the client.
    /// </summary>
    public string Ip { get; } = Context.Request.RemoteEndPoint.Address.ToString();

    /// <summary>
    /// Gets or sets a value indicating whether the client
    /// is ready to receive the game state.
    /// </summary>
    public bool IsReadyToReceiveGameState
    {
        get
        {
            lock (this.readyLock)
            {
                return this.isReadyToReceiveGameState;
            }
        }

        set
        {
            lock (this.readyLock)
            {
                this.isReadyToReceiveGameState = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the time when the last ping was sent.
    /// </summary>
    public DateTime LastPingSentTime
    {
        get
        {
            lock (this.pingSentLock)
            {
                return this.lastPingSendTime;
            }
        }

        set
        {
            lock (this.pingSentLock)
            {
                this.lastPingSendTime = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the second ping attempt is sent.
    /// </summary>
    public bool IsSecondPingAttempt
    {
        get
        {
            lock (this.pingSentLock)
            {
                return this.isSecondPingAttempt;
            }
        }

        set
        {
            lock (this.pingSentLock)
            {
                this.isSecondPingAttempt = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a player has sent a pong.
    /// </summary>
    /// <remarks>
    /// This property is thread-safe.
    /// </remarks>
    public bool HasSentPong
    {
        get
        {
            lock (this.hasSentPongLock)
            {
                return this.hasSentPong;
            }
        }

        set
        {
            lock (this.hasSentPongLock)
            {
                this.hasSentPong = value;
            }
        }
    }

    /// <summary>
    /// Closes the connection.
    /// </summary>
    /// <param name="status">The status of the close.</param>
    /// <param name="description">The description of the close.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CloseAsync(
        WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure,
        string description = "Closing",
        CancellationToken cancellationToken = default)
    {
        await this.SendPacketSemaphore.WaitAsync(cancellationToken);

        try
        {
            this.Logger.Debug(
                "Closing connection: {status}, {description}, {connection}",
                status,
                description,
                this);

            await this.Socket.CloseAsync(status, description, cancellationToken);
        }
        catch (Exception ex)
        {
            this.Logger.Error(
                ex,
                "Error while closing the connection: {status}, {description}, {connection}",
                status,
                description,
                this);
        }
        finally
        {
            _ = this.SendPacketSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"IP={this.Ip}, SocketState={this.Socket.State}";
    }
}
