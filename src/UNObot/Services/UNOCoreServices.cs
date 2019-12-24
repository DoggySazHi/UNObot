using System;
using System.Threading.Tasks;
using System.Timers;
using System.Collections.Generic;
using Discord.WebSocket;
using Newtonsoft.Json;
using UNObot.TerminalCore;

namespace UNObot.Modules
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Help : Attribute
    {
        public string[] Usages { get; set; }
        public string HelpMsg { get; set; }
        public bool Active { get; set; }
        public string Version { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:UNObot.Modules.Help"/> class.
        /// </summary>
        /// <param name="Usages">Usages of the command.</param>
        /// <param name="HelpMsg">Help message.</param>
        /// <param name="Active">Check if command should be displayed in the help list.</param>
        /// <param name="Version">Version when the command was first introduced.</param>
        [JsonConstructor]
        public Help(string[] Usages, string HelpMsg, bool Active, string Version)
        {
            this.Usages = Usages;
            this.HelpMsg = HelpMsg;
            this.Active = Active;
            this.Version = Version;
        }
    }

    public class Command
    {
        public string CommandName { get; set; }
        public List<string> Usages { get; set; }
        public List<string> Aliases { get; set; }
        public string Help { get; set; }
        public bool Active { get; set; }
        public string Version { get; set; }

        [JsonConstructor]
        public Command(string CommandName, List<string> Aliases, List<string> Usages, string Help, bool Active, string Version)
        {
            this.CommandName = CommandName;
            this.Aliases = Aliases;
            this.Usages = Usages;
            this.Help = Help;
            this.Active = Active;
            this.Version = Version;
        }
    }

    public class Card
    {
        public string Color;
        public string Value;

        public override string ToString() => $"{Color} {Value}";

        public bool Equals(Card other) => Value == other.Value && Color == other.Color;
    }

    public class ServerCard
    {
        public Card Card;
        public int CardsAvailable = 1;
        public int CardsAllocated = 1;

        public ServerCard(Card Card)
            => this.Card = Card;
        public bool Equals(ServerCard other) => Card.Equals(other.Card);
    }

    public class ServerDeck
    {
        public List<ServerCard> Cards;
        public static readonly List<string> Colors = new List<string> { "Red", "Green", "Blue", "Yellow" };
        public static readonly List<string> Values = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "Skip", "Reverse", "+2" };

        public ServerDeck()
        {
            //smells like spaghetti code
            for (int i = 0; i < 2; i++)
                foreach (string Color in Colors)
                    foreach (string Value in Values)
                        Cards.Add(new ServerCard(new Card
                        {
                            Color = Color,
                            Value = Value
                        }));
            foreach (string Color in Colors)
                Cards.Add(new ServerCard(new Card
                {
                    Color = Color,
                    Value = "0"
                }));
            for (int i = 0; i < 4; i++)
            {
                Cards.Add(new ServerCard(new Card
                {
                    Color = "Wild",
                    Value = "Color"
                }));
                Cards.Add(new ServerCard(new Card
                {
                    Color = "Wild",
                    Value = "+4"
                }));
            }
        }

        public void Shuffle()
        {
            Cards.Shuffle();
        }
    }

    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + System.Threading.Thread.CurrentThread.ManagedThreadId))); }
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public static class UNOCoreServices
    {
        public static Card RandomCard()
        {
            Card card = new Card();
            object lockObject = new object();
            int myColor = 1;
            int myCard = 0;
            lock (lockObject)
            {
                //0-9 is number, 10 is actioncard
                myCard = ThreadSafeRandom.ThisThreadsRandom.Next(0, 11);
                // see switch
                myColor = ThreadSafeRandom.ThisThreadsRandom.Next(1, 5);
            }

            card.Color = myColor switch
            {
                1 => "Red",
                2 => "Yellow",
                3 => "Green",
                _ => "Blue",
            };
            if (myCard < 10)
            {
                card.Value = myCard.ToString();
            }
            else
            {
                //4 is wild, 1-3 is action
                int action = ThreadSafeRandom.ThisThreadsRandom.Next(1, 5);
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
                        int wild = ThreadSafeRandom.ThisThreadsRandom.Next(1, 3);
                        card.Color = "Wild";
                        if (wild == 1)
                            card.Value = "Color";
                        else
                            card.Value = "+4";
                        break;
                    default:
                        _ = 1;
                        break;
                }
            }
            Console.WriteLine(card.Color + " " + card.Value + " " + myColor + " " + myCard);
            return card;
        }
    }

    public static class AFKtimer
    {
        public static Dictionary<ulong, Timer> playTimers = new Dictionary<ulong, Timer>();

        public static void ResetTimer(ulong server)
        {
            if (!playTimers.ContainsKey(server))
                ColorConsole.WriteLine("ERROR: Attempted to reset timer that doesn't exist!", ConsoleColor.Red);
            else
            {
                playTimers[server].Stop();
                playTimers[server].Start();
            }
        }
        public static void StartTimer(ulong server)
        {
            Console.WriteLine("Starting timer!");
            if (playTimers.ContainsKey(server))
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
        async static void TimerOver(object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Timer over!");
            ulong serverID = 0;
            foreach (ulong server in playTimers.Keys)
            {
                Timer timer = (Timer)source;
                if (timer.Equals(playTimers[server]))
                    serverID = server;
            }
            if (serverID == 0)
            {
                ColorConsole.WriteLine("ERROR: Couldn't figure out what server timer belonged to!", ConsoleColor.Yellow);
                return;
            }
            ulong currentPlayer = await QueueHandlerService.GetCurrentPlayer(serverID);
            await UNODatabaseService.RemoveUser(currentPlayer);
            await QueueHandlerService.DropFrontPlayer(serverID);
            Console.WriteLine("SayPlayer");
            await Program.SendMessage($"<@{currentPlayer}>, you have been AFK removed.\n", serverID);
            await Program.SendPM("You have been AFK removed.", currentPlayer);
            if (await QueueHandlerService.PlayerCount(serverID) == 0)
            {
                await UNODatabaseService.ResetGame(serverID);
                await Program.SendMessage("Game has been reset, due to nobody in-game.", serverID);
                DeleteTimer(serverID);
                return;
            }
            ResetTimer(serverID);
            await Program.SendMessage($"It is now <@{await QueueHandlerService.GetCurrentPlayer(serverID)}> turn.\n", serverID);
        }

        public static void DeleteTimer(ulong server)
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