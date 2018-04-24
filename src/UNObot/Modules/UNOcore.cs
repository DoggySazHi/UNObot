using System;
using System.Threading.Tasks;
using System.Timers;
namespace DiscordBot.Modules
{
    public class Card
    {
        public string Color;
        public string Value;

        public override String ToString() => $"{Color} {Value}";

        public bool Equals(Card other) => Value == other.Value && Color == other.Color;
    }
    public static class UNOcore
    {
        public static Random r = new Random();

        public static Card RandomCard()
        {
            Card card = new Card();
            Object lockObject = new object();
            int myColor = 1;
            int myCard = 0;
            lock(lockObject)
            {
                //0-9 is number, 10 is actioncard
                myColor = r.Next(0, 11);
                // see switch
                myCard = r.Next(1, 5);
            }

            switch (myColor)
            {
                case 1:
                    card.Color = "Red";
                    break;
                case 2:
                    card.Color = "Yellow";
                    break;
                case 3:
                    card.Color = "Green";
                    break;
                case 4:
                    card.Color = "Blue";
                    break;
            }

            if (myCard < 10)
            {
                card.Value = myCard.ToString();
            }
            else
            {
                //4 is wild, 1-3 is action
                int action = r.Next(1, 5);
                switch (action)
                {
                    case 1:
                        card.Value = "Skip";
                        break;
                    case 2:
                        card.Value = "Reverse";
                        break;
                    case 3:
                        card.Value = "+2";
                        break;
                    case 4:
                        int wild = r.Next(1, 3);
                        card.Color = "Wild";
                        if (wild == 1)
                            card.Value = "Color";
                        else
                            card.Value = "+4";
                        break;
                }
            }
            return card;
        }
    }
    public class ColorConsole
    {
        public static void WriteLine(string Text, string Color)
        {
            ConsoleColor[] colors = (ConsoleColor[]) ConsoleColor.GetValues(typeof(ConsoleColor));
            ConsoleColor behind = Console.BackgroundColor;
            ConsoleColor front = Console.ForegroundColor;
            bool colorFound = false;
            foreach(var color in colors)
            {
                if(color.ToString() == Color)
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = color;
                    colorFound |= true;
                    break;
                }
            }
            Console.WriteLine(Text);
            Console.BackgroundColor = behind;
            Console.ForegroundColor = front;
            #if DEBUG
            if(!colorFound)
                WriteLine("[WARN] Attempted to WriteLine with a color that doesn't exist!", ConsoleColor.Yellow);
            #endif
        }
        public static void WriteLine(string Text, ConsoleColor color)
        {
            ConsoleColor behind = Console.BackgroundColor;
            ConsoleColor front = Console.ForegroundColor;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = color;
            Console.WriteLine(Text);
            Console.BackgroundColor = behind;
            Console.ForegroundColor = front;
        }
    }
    public class GameTimer : Timer
    {
        public new bool Disposed { get; set; }
        protected override void Dispose(bool disposing) {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
    public class AFKtimer
    {
        static GameTimer playTimer;
        void ResetTimer()
        {
            playTimer.Stop();
            playTimer.Start();
        }
        void StartTimer()
        {
            playTimer = (GameTimer) new Timer(90000);
            playTimer.AutoReset = false;
            playTimer.Elapsed += TimerOver;
            playTimer.Start();
        }
        private static void TimerOver(Object source, ElapsedEventArgs e)
        {
            //TODO write something here nerd
        }
        void DeleteTimer()
        {
            if(playTimer.Disposed)
                ColorConsole.WriteLine("[WARN] Attempted to dispose a timer that was already disposed!", ConsoleColor.Yellow);
            else
                playTimer.Dispose();
        }
    }
}