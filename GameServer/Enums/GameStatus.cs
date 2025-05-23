﻿namespace GameServer;

/// <summary>
/// An enumeration representing the game status.
/// </summary>
internal enum GameStatus
{
    /// <summary>
    /// Represents the game is in the lobby.
    /// </summary>
    InLobby,

    /// <summary>
    /// Represents the game is starting.
    /// </summary>
    Starting,

    /// <summary>
    /// Represents the game is in progress.
    /// </summary>
    Running,

    /// <summary>
    /// Represents the game has ended.
    /// </summary>
    Ended,
}
