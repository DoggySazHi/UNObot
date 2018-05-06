using System;
using System.Threading.Tasks;
using System.Timers;
using System.Collections.Generic;
namespace UNObot.Modules
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
        public ulong Server { get; set; }
        public new bool Disposed { get; set; }
        protected override void Dispose(bool disposing) {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
    public class AFKtimer
    {
        public Dictionary<ulong,GameTimer> playTimers = new Dictionary<ulong,GameTimer>();
        UNOdb db = new UNOdb();
        QueueHandler queueHandler = new QueueHandler();
        public void ResetTimer(ulong server)
        {
            if(!playTimers.ContainsKey(server))
                ColorConsole.WriteLine("ERROR: Attempted to reset timer that doesn't exist!", ConsoleColor.Red);
            else
            {
                playTimers[server].Stop();
                playTimers[server].Start();
            }
        }
        public void StartTimer(ulong server)
        {
            if(playTimers.ContainsKey(server))
                ColorConsole.WriteLine("WARNING: Attempted to start timer that already existed!", ConsoleColor.Yellow);
            else
            {
                playTimers[server] = (GameTimer) new Timer(90000);
                playTimers[server].Server = server;
                playTimers[server].AutoReset = false;
                playTimers[server].Elapsed += TimerOver;
                playTimers[server].Start();
            }
        }
        private async void TimerOver(Object source, ElapsedEventArgs e)
        {
            ulong serverID = 0;
            foreach (ulong server in playTimers.Keys)
            {
                if(playTimers[server].Equals(source))
                    serverID = server;
            }
            if(serverID == 0)
            {
                ColorConsole.WriteLine("ERROR: Couldn't figure out what server timer belonged to!", ConsoleColor.Yellow);
                return;
            }
            ulong currentPlayer = await queueHandler.GetCurrentPlayer(serverID);
            await db.RemoveUser(currentPlayer);
            await PlayCard.ReplyAsync($"<@{currentPlayer}>, you have been AFK removed.\n");
            await queueHandler.NextPlayer(serverID);
            ResetTimer(serverID);
            //reupdate
            if (await queueHandler.PlayerCount(serverID) == 0)
            {
                await db.ResetGame(serverID);
                await PlayCard.ReplyAsync("Game has been reset, due to nobody in-game.");
                DeleteTimer(serverID);
            }
            else
                await PlayCard.ReplyAsync($"It is now <@{queueHandler.GetCurrentPlayer(serverID)}> turn.\n");
        }
        public void DeleteTimer(ulong server)
        {
            if(playTimers[server].Disposed)
                ColorConsole.WriteLine("[WARN] Attempted to dispose a timer that was already disposed!", ConsoleColor.Yellow);
            else
                playTimers[server].Dispose();
            playTimers.Remove(server);
        }
    }
}