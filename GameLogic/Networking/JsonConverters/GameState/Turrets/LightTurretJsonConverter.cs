﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameLogic.Networking.GameState;

#if STEREO

/// <summary>
/// Represents a light turret JSON converter.
/// </summary>
/// <param name="context">The serialization context.</param>
internal class LightTurretJsonConverter(GameSerializationContext context)
    : JsonConverter<LightTurret>
{
    /// <inheritdoc/>
    public override LightTurret? ReadJson(JsonReader reader, Type objectType, LightTurret? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);

        var direction = JsonConverterUtils.ReadEnum<Direction>(jObject["direction"]!, context.EnumSerialization);
        var turret = new LightTurret(direction);

        if (context is GameSerializationContext.Spectator || jObject["bulletCount"] is not null)
        {
            turret.Bullet = new BulletAbility(null!)
            {
                Count = jObject["bulletCount"]!.Value<int>(),
                RemainingRegenerationTicks = jObject["ticksToBullet"]!.Value<int?>(),
            };

            turret.DoubleBullet = new DoubleBulletAbility(null!)
            {
                RemainingRegenerationTicks = jObject["ticksToDoubleBullet"]!.Value<int?>(),
            };

            turret.HealingBullet = new HealingBulletAbility(null!)
            {
                RemainingRegenerationTicks = jObject["ticksToHealingBullet"]!.Value<int?>(),
            };

            turret.StunBullet = new StunBulletAbility(null!)
            {
                RemainingRegenerationTicks = jObject["ticksToStunBullet"]!.Value<int?>(),
            };
        }

        return turret;
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, LightTurret? value, JsonSerializer serializer)
    {
        var jObject = new JObject
        {
            ["direction"] = JsonConverterUtils.WriteEnum(value!.Direction, context.EnumSerialization),
        };

        var isSpectator = context is GameSerializationContext.Spectator;
        var isOwner = !isSpectator && context.IsPlayerWithId(value.Tank.Owner.Id);
        var isTeammate = !isOwner && context.IsTeammate(value.Tank.Owner.Id);

        if (isSpectator || isOwner || isTeammate)
        {
            jObject["bulletCount"] = value.Bullet!.Count;
            jObject["ticksToBullet"] = value.Bullet.RemainingRegenerationTicks;
            jObject["ticksToDoubleBullet"] = value.DoubleBullet!.RemainingRegenerationTicks;
            jObject["ticksToHealingBullet"] = value.HealingBullet!.RemainingRegenerationTicks;
            jObject["ticksToStunBullet"] = value.StunBullet!.RemainingRegenerationTicks;
        }

        jObject.WriteTo(writer);
    }
}

#endif
