using System;
namespace UNObot.TerminalCore
{
    public static class ColorConsole
    {
        [Obsolete("Use a ConsoleColor instead, it's more reliable.")]
        public static void Write(string Text, string Color, string BGColor)
        {
            Color = Color.Trim().ToLower();
            BGColor = BGColor.Trim().ToLower();

            ConsoleColor[] colors = (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor));
            bool colorFound = false;
            bool bgColorFound = false;
            foreach (var color in colors)
            {
                if (color.ToString().ToLower() == Color)
                {
                    Console.ForegroundColor = color;
                    colorFound = true;
                }
                if (color.ToString().ToLower() == BGColor)
                {
                    Console.BackgroundColor = color;
                    bgColorFound = true;
                }
            }
            Console.Write(Text);
            Console.ResetColor();

#if DEBUG
            //Warn!
            if (!colorFound || !bgColorFound)
                WriteLine("[WARN] Attempted to WriteLine with a color that doesn't exist!", ConsoleColor.Yellow);
#endif
        }

        public static void Write(string Text, ConsoleColor Color, ConsoleColor BGColor)
        {
            Console.BackgroundColor = BGColor;
            Console.ForegroundColor = Color;
            Console.Write(Text);
            Console.ResetColor();
        }

        [Obsolete("Use a ConsoleColor instead, it's more reliable.")]
        public static void Write(string Text, string Color)
        {
            Write(Text, Color, Console.BackgroundColor.ToString());
        }

        public static void Write(string Text, ConsoleColor Color)
        {
            Write(Text, Color, Console.BackgroundColor);
        }

        [Obsolete("Use a ConsoleColor instead, it's more reliable.")]
        public static void WriteLine(string Text, string Color)
        {
            Write(Text, Color);
            Console.WriteLine();
        }

        public static void WriteLine(string Text, ConsoleColor Color)
        {
            Write(Text, Color);
            Console.WriteLine();
        }

        [Obsolete("Use a ConsoleColor instead, it's more reliable.")]
        public static void WriteLine(string Text, string Color, string BGColor)
        {
            Write(Text, Color, BGColor);
            Console.WriteLine();
        }

        public static void WriteLine(string Text, ConsoleColor Color, ConsoleColor BGColor)
        {
            Write(Text, Color, BGColor);
            Console.WriteLine();
        }
    }
}
