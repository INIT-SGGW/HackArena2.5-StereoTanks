﻿using System.Diagnostics;
using GameLogic.Networking.Map;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GameLogic.Networking.GameState;

/// <summary>
/// Represents a grid state json converter.
/// </summary>
/// <param name="context">The serialization context.</param>
internal class GridTilesJsonConverter(GameSerializationContext context) : JsonConverter<TilesPayload>
{
    /// <inheritdoc/>
    public override TilesPayload? ReadJson(JsonReader reader, Type objectType, TilesPayload? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        switch (context)
        {
            case GameSerializationContext.Player:
                return this.ReadPlayerJson(reader, serializer);
            case GameSerializationContext.Spectator:
                return this.ReadSpectatorJson(reader, serializer);
            default:
                Debug.Fail("Unknown serialization context.");
                return null;
        }
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, TilesPayload? value, JsonSerializer serializer)
    {
        switch (context)
        {
            case GameSerializationContext.Player:
                this.WritePlayerJson(writer, value, serializer);
                break;
            case GameSerializationContext.Spectator:
                this.WriteSpectatorJson(writer, value, serializer);
                break;
            default:
                Debug.Fail("Unknown serialization context.");
                break;
        }
    }

    private TilesPayload ReadPlayerJson(JsonReader reader, JsonSerializer serializer)
    {
        var jObject = JArray.Load(reader);

        var wallGrid = new Wall?[jObject.Count, (jObject[0] as JArray)!.Count];
        List<Tank> tanks = [];
        List<Bullet> bullets = [];
        List<Laser> lasers = [];
        List<Mine> mines = [];
#if !STEREO
        List<SecondaryItem> items = [];
#endif

        for (int i = 0; i < jObject.Count; i++)
        {
            var row = jObject[i] as JArray;
            for (int j = 0; j < row!.Count; j++)
            {
                var cell = (row[j] as JArray)!;
                foreach (var item in cell)
                {
                    switch (item["type"]?.Value<string>())
                    {
                        case "wall":
                            var wallPayload = item["payload"] ?? new JObject();
                            wallPayload["x"] = i;
                            wallPayload["y"] = j;
                            var wall = wallPayload.ToObject<Wall>(serializer);
                            wallGrid[i, j] = wall;
                            break;
                        case "tank":
                            var tank = item["payload"]!.ToObject<Tank>(serializer)!;
                            tank.SetPosition(i, j);
                            tanks.Add(tank);
                            break;
                        case "bullet":
                            var bulletPayload = item["payload"] ?? new JObject();
                            bulletPayload["x"] = i;
                            bulletPayload["y"] = j;
                            var bullet = bulletPayload.ToObject<Bullet>(serializer)!;
                            bullets.Add(bullet);
                            break;
                        case "laser":
                            var laserPayload = item["payload"] ?? new JObject();
                            laserPayload["x"] = i;
                            laserPayload["y"] = j;
                            var laser = laserPayload.ToObject<Laser>(serializer)!;
                            lasers.Add(laser);
                            break;
                        case "mine":
                            var minePayload = item["payload"] ?? new JObject();
                            minePayload["x"] = i;
                            minePayload["y"] = j;
                            var mine = minePayload.ToObject<Mine>(serializer)!;
                            mines.Add(mine);
                            break;
#if !STEREO
                        case "item":
                            var itemPayload = item["payload"]!;
                            itemPayload["x"] = i;
                            itemPayload["y"] = j;
                            var mapItem = itemPayload.ToObject<SecondaryItem>(serializer)!;
                            items.Add(mapItem);
                            break;
#endif
                    }
                }
            }
        }

        return new TilesPayload(wallGrid, tanks, bullets, lasers, mines)
#if !STEREO
            {
                Items = items,
            }
#endif
            ;
    }

    private TilesPayload ReadSpectatorJson(JsonReader reader, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);

        var gridDimensions = jObject["gridDimensions"]!.ToObject<int[]>()!;
        var wallGrid = new Wall?[gridDimensions[0], gridDimensions[1]];
        foreach (var wall in jObject["walls"]!.ToObject<List<Wall>>(serializer)!)
        {
            wallGrid[wall.X, wall.Y] = wall;
        }

        var tanks = jObject["tanks"]!.ToObject<List<Tank>>(serializer)!;
        var bullets = jObject["bullets"]!.ToObject<List<Bullet>>(serializer)!;
        var lasers = jObject["lasers"]!.ToObject<List<Laser>>(serializer)!;
        var mines = jObject["mines"]!.ToObject<List<Mine>>(serializer)!;
#if !STEREO
        var items = jObject["items"]!.ToObject<List<SecondaryItem>>(serializer)!;
#endif

        return new TilesPayload(wallGrid, tanks, bullets, lasers, mines)
#if !STEREO
            {
                Items = items,
            }
#endif
            ;
    }

    private void WritePlayerJson(JsonWriter writer, TilesPayload? value, JsonSerializer serializer)
    {
        var playerContext = (context as GameSerializationContext.Player)!;
        var visibilityGrid = playerContext.VisibilityGrid;

        bool IsVisible(int x, int y)
        {
            return playerContext.IsUsingRadar is true
                || FogOfWarManager.IsElementVisible(visibilityGrid, x, y);
        }

        var jObject = new JArray();

        for (int i = 0; i < value!.WallGrid.GetLength(0); i++)
        {
            var row = new JArray();
            for (int j = 0; j < value.WallGrid.GetLength(1); j++)
            {
                var cell = new JArray();

                if (value.WallGrid[i, j] is { } w)
                {
                    var obj = JObject.FromObject(w, serializer);
                    var wall = new JObject()
                    {
                        { "type", "wall" },
                        { "payload",  obj },
                    };
                    cell.Add(wall);
                }

                row.Add(cell);
            }

            jObject.Add(row);
        }

        foreach (Tank tank in value.Tanks.Where(x => !x.IsDead))
        {
            if (!IsVisible(tank.X, tank.Y))
            {
                continue;
            }

            var obj = JObject.FromObject(tank, serializer);
            (jObject[tank.X][tank.Y] as JArray)!.Add(new JObject
            {
                { "type", "tank" },
                { "payload", obj },
            });
        }

        foreach (Bullet bullet in value.Bullets)
        {
            if (!IsVisible(bullet.X, bullet.Y))
            {
                continue;
            }

            var obj = JObject.FromObject(bullet, serializer);
            (jObject[bullet.X][bullet.Y] as JArray)!.Add(new JObject
            {
                { "type", "bullet" },
                { "payload", obj },
            });
        }

        foreach (Laser laser in value.Lasers)
        {
            if (!IsVisible(laser.X, laser.Y))
            {
                continue;
            }

            var obj = JObject.FromObject(laser, serializer);
            (jObject[laser.X][laser.Y] as JArray)!.Add(new JObject
            {
                { "type", "laser" },
                { "payload", obj },
            });
        }

        foreach (Mine mine in value.Mines)
        {
            if (!IsVisible(mine.X, mine.Y))
            {
                continue;
            }

            var obj = JObject.FromObject(mine, serializer);
            (jObject[mine.X][mine.Y] as JArray)!.Add(new JObject
            {
                { "type", "mine" },
                { "payload", obj },
            });
        }

#if !STEREO

        foreach (SecondaryItem item in value.Items)
        {
            if (!IsVisible(item.X, item.Y))
            {
                continue;
            }

            var isCovered = value.Tanks.Any(t => t.X == item.X && t.Y == item.Y)
                || value.Mines.Any(m => m.X == item.X && m.Y == item.Y);

            var itemToSerialize = isCovered
                ? new SecondaryItem(item.X, item.Y, SecondaryItemType.Unknown)
                : item;

            var obj = JObject.FromObject(itemToSerialize, serializer);
            (jObject[item.X][item.Y] as JArray)!.Add(new JObject
            {
                { "type", "item" },
                { "payload", obj },
            });
        }

#endif

        jObject.WriteTo(writer);
    }

    private void WriteSpectatorJson(JsonWriter writer, TilesPayload? value, JsonSerializer serializer)
    {
        var jObject = new JObject
        {
            ["gridDimensions"] = new JArray(value!.WallGrid.GetLength(0), value.WallGrid.GetLength(1)),
            ["walls"] = JArray.FromObject(value.WallGrid.Cast<Wall?>().Where(w => w is not null), serializer),
            ["tanks"] = JArray.FromObject(value.Tanks, serializer),
            ["bullets"] = JArray.FromObject(value.Bullets, serializer),
            ["lasers"] = JArray.FromObject(value.Lasers, serializer),
            ["mines"] = JArray.FromObject(value.Mines, serializer),
#if !STEREO
            ["items"] = JArray.FromObject(value.Items, serializer),
#endif
        };

        jObject.WriteTo(writer);
    }
}
