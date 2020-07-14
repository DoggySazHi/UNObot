using System;

namespace UNObot.Plugins.TerminalCore
{
    static class ColorConsole
    {
        [Obsolete("Use a ConsoleColor instead, it's more reliable.")]
        private static void Write(string text, string colorText, string bgColor)
        {
            colorText = colorText.Trim().ToLower();
            bgColor = bgColor.Trim().ToLower();

            var colors = (ConsoleColor[]) Enum.GetValues(typeof(ConsoleColor));
            var colorFound = false;
            var bgColorFound = false;
            foreach (var color in colors)
            {
                if (color.ToString().ToLower() == colorText)
                {
                    Console.ForegroundColor = color;
                    colorFound = true;
                }

                if (color.ToString().ToLower() == bgColor)
                {
                    Console.BackgroundColor = color;
                    bgColorFound = true;
                }
            }

            Console.Write(text);
            Console.ResetColor();

#if DEBUG
            //Warn!
            if (!colorFound || !bgColorFound)
                WriteLine("[WARN] Attempted to WriteLine with a color that doesn't exist!", ConsoleColor.Yellow);
#endif
        }

        private static void Write(string text, ConsoleColor color, ConsoleColor bgColor)
        {
            Console.BackgroundColor = bgColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ResetColor();
        }

        [Obsolete("Use a ConsoleColor instead, it's more reliable.")]
        private static void Write(string text, string color)
        {
            Write(text, color, Console.BackgroundColor.ToString());
        }

        internal static void Write(string text, ConsoleColor color)
        {
            Write(text, color, Console.BackgroundColor);
        }

        [Obsolete("Use a ConsoleColor instead, it's more reliable.")]
        internal static void WriteLine(string text, string color)
        {
            Write(text, color);
            Console.WriteLine();
        }

        internal static void WriteLine(string text, ConsoleColor color)
        {
            Write(text, color);
            Console.WriteLine();
        }

        [Obsolete("Use a ConsoleColor instead, it's more reliable.")]
        internal static void WriteLine(string text, string color, string bgColor)
        {
            Write(text, color, bgColor);
            Console.WriteLine();
        }

        internal static void WriteLine(string text, ConsoleColor color, ConsoleColor bgColor)
        {
            Write(text, color, bgColor);
            Console.WriteLine();
        }
    }
}