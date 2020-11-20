using System;

namespace ConnectBot.Templates
{
    [Flags]
    public enum GameMode : byte
    {
        Normal = 0,

        /// <summary>
        ///     The embed doesn't show anything. It just tells you if it's illegal.
        /// </summary>
        Blind = 1,

        /// <summary>
        ///     Uses the "Default Board" stated in the user's settings, versus the normal 7x6 and C4 options.
        /// </summary>
        Custom = 2,
    }
}