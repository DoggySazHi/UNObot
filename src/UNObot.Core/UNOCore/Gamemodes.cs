using System;

namespace UNObot.Core.UNOCore;

[Flags]
public enum GameMode : byte
{
    Normal = 0,

    /// <summary>
    ///     .game does not show the amount of cards each player has.
    /// </summary>
    Private = 1,

    /// <summary>
    ///     .skip allows for the user to draw two cards.
    /// </summary>
    Fast = 2,

    /// <summary>
    ///     .draw is limited to only one usage, with .skip moving on. .quickplay is affected.
    /// </summary>
    Retro = 4,

    /// <summary>
    ///     .uno can be used by a person without UNO to call out if someone else does have an UNO.
    /// </summary>
    UNOCallout = 8,
        
    /// <summary>
    ///     Allows function cards to be stacked upon each other.
    /// </summary>
    Stack = 16,
        
    /// <summary>
    ///     Allows for the swapping of cards between players in special conditions.
    /// </summary>
    SevenZero = 32,
        
    /// <summary>
    ///     Allows for the challenging of Wild +4 cards.
    /// </summary>
    ChallengeWild = 64,
        
    /// <summary>
    ///     Use the full 108 card deck in UNO, rather than RNG.
    /// </summary>
    UseFullDeck = 128
}