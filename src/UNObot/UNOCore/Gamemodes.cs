using System;

namespace UNObot.UNOCore
{
    [Flags]
    internal enum GameMode : byte
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
        UNOCallout = 8
    }
}