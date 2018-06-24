using System;
using System.Threading.Tasks;
using System.Timers;
using System.Collections.Generic;
using Discord.WebSocket;

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
        static UNOdb db = new UNOdb();
        public static Random r = new Random();

        public static Card RandomCard()
        {
            Card card = new Card();
            object lockObject = new object();
            int myColor = 1;
            int myCard = 0;
            lock(lockObject)
            {
                //0-9 is number, 10 is actioncard
                myCard = r.Next(0, 11);
                // see switch
                myColor = r.Next(1, 5);
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
            Console.WriteLine(card.Color + " " + card.Value + " " + myColor + " " + myCard);
            return card;
        }
    }
    public static class ColorConsole
    {
        public static void WriteLine(string Text, string Color)
        {
            ConsoleColor[] colors = (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor));
            //Ignore warning; debug mode.
            bool colorFound = false;
            foreach(var color in colors)
            {
                if(color.ToString() == Color)
                {
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = color;
                    colorFound = true;
                    break;
                }
            }
            Console.WriteLine(Text);
            Console.ResetColor();
            #if DEBUG
            if(!colorFound)
                WriteLine("[WARN] Attempted to WriteLine with a color that doesn't exist!", ConsoleColor.Yellow);
            #endif
        }
        public static void WriteLine(string Text, ConsoleColor color)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = color;
            Console.WriteLine(Text);
            Console.ResetColor();
        }
    }
    public class AFKtimer
    {
        public static Dictionary<ulong,Timer> playTimers = new Dictionary<ulong,Timer>();

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
            Console.WriteLine("Starting timer!");
            if(playTimers.ContainsKey(server))
                ColorConsole.WriteLine("WARNING: Attempted to start timer that already existed!", ConsoleColor.Yellow);
            else
            {
                playTimers[server] = new Timer
                {
                    Interval = 90000,
                    AutoReset = false
                };
                playTimers[server].Elapsed += TimerOver;
                playTimers[server].Start();
            }
        }
        async void TimerOver(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Timer over!");
            ulong serverID = 0;
            foreach(ulong server in playTimers.Keys)
            {
                Timer timer = (Timer)source;
                if (timer.Equals(playTimers[server]))
                    serverID = server;
            }
            if(serverID == 0)
            {
                ColorConsole.WriteLine("ERROR: Couldn't figure out what server timer belonged to!", ConsoleColor.Yellow);
                return;
            }
            ulong currentPlayer = await queueHandler.GetCurrentPlayer(serverID);
            await db.RemoveUser(currentPlayer);
            await queueHandler.RemovePlayer(currentPlayer, serverID);
            Console.WriteLine("SayPlayer");
            await Program.SendMessage($"<@{currentPlayer}>, you have been AFK removed.\n", serverID);
            await Program.SendPM("You have been AFK removed.", currentPlayer);
            if (await queueHandler.PlayerCount(serverID) == 1)
            {
                await db.ResetGame(serverID);
                await Program.SendMessage("Game has been reset, due to nobody in-game.", serverID);
                DeleteTimer(serverID);
                return;
            }
            Console.WriteLine("NextPlayer");
            await queueHandler.NextPlayer(serverID);
            Console.WriteLine("ResetPlayer");
            ResetTimer(serverID);
            //reupdate

            //else
            await Program.SendMessage($"It is now <@{queueHandler.GetCurrentPlayer(serverID)}> turn.\n", serverID);
        }

        public void DeleteTimer(ulong server)
        {
            if (playTimers.ContainsKey(server))
            {
                if (playTimers[server] == null)
                    ColorConsole.WriteLine("[WARN] Attempted to dispose a timer that was already disposed!", ConsoleColor.Yellow);
                else
                    playTimers[server].Dispose();
            }
            playTimers.Remove(server);
        }
    }
}