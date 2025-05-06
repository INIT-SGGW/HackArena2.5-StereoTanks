﻿namespace GameLogic;

#if !STEREO

/// <summary>
/// Represents a secondary item on the map.
/// </summary>
/// <param name="X">The x coordinate of the item.</param>
/// <param name="Y">The y coordinate of the item.</param>
/// <param name="Type">The type of the item.</param>
public record class SecondaryItem(int X, int Y, SecondaryItemType Type);

#endif
