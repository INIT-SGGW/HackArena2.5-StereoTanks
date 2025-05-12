﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameLogic.Networking.GameState;

/// <summary>
/// Represents a bullet json converter.
/// </summary>
/// <param name="context">The serialization context.</param>
internal class BulletJsonConverter(GameSerializationContext context) : JsonConverter<Bullet>
{
    /// <inheritdoc/>
    public override Bullet? ReadJson(JsonReader reader, Type objectType, Bullet? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);

        var id = jObject["id"]!.Value<int>()!;
        var x = jObject["x"]!.Value<int>();
        var y = jObject["y"]!.Value<int>();
        var speed = jObject["speed"]!.Value<float>()!;
        var direction = JsonConverterUtils.ReadEnum<Direction>(jObject["direction"]!);
        var type = JsonConverterUtils.ReadEnum<BulletType>(jObject["type"]!);

        if (context is GameSerializationContext.Player)
        {
            return new Bullet(id, x, y, direction, type, speed);
        }

        var damage = jObject["damage"]!.Value<int>();
        var shooterId = jObject["shooterId"]!.Value<string>()!;

        return new Bullet(id, x, y, direction, type, speed, damage, shooterId);
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, Bullet? value, JsonSerializer serializer)
    {
        var jObject = new JObject
        {
            ["id"] = value!.Id,
            ["speed"] = value!.Speed,
            ["direction"] = JsonConverterUtils.WriteEnum(value!.Direction, context.EnumSerialization),
            ["type"] = JsonConverterUtils.WriteEnum(value!.Type, context.EnumSerialization),
        };

        if (context is GameSerializationContext.Spectator)
        {
            jObject["x"] = value.X;
            jObject["y"] = value.Y;
            jObject["damage"] = value.Damage;
            jObject["shooterId"] = value.ShooterId;
        }

        jObject.WriteTo(writer);
    }
}
