using System;
using System.Collections.Generic;
using System.Timers;
using Discord;
using Newtonsoft.Json;

namespace UNObot.Services
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Help : Attribute
    {
        public string[] Usages { get; }
        public string HelpMsg { get; }
        public bool Active { get; }
        public string Version { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:UNObot.Services.Help"/> class.
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

    [AttributeUsage(AttributeTargets.Method)]
    public class DisableDMs : Attribute
    {
        public bool Disabled { get; }

        public bool Enabled => !Disabled;

        [JsonConstructor]
        public DisableDMs()
        {
            Disabled = true;
        }

        [JsonConstructor]
        public DisableDMs(bool Disabled)
        {
            this.Disabled = Disabled;
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
        public bool DisableDMs { get; set; }

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

        [JsonConstructor]
        public Command(string CommandName, List<string> Aliases, List<string> Usages, string Help, bool Active, string Version, bool DisableDMs) : this(CommandName, Aliases, Usages, Help, Active, Version)
        {
            this.DisableDMs = DisableDMs;
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
        [Flags]
        public enum Gamemodes
        {
            Normal = 0,
            /// <summary>
            /// .game does not show the amount of cards each player has.
            /// </summary>
            Private = 1,
            /// <summary>
            /// .skip allows for the user to draw two cards.
            /// </summary>
            Fast = 2,
            /// <summary>
            /// .draw is limited to only one usage, with .skip moving on. .quickplay is affected.
            /// </summary>
            Retro = 4,
            /// <summary>
            /// .uno can be used by a person without UNO to call out if someone else does have an UNO.
            /// </summary>
            UNOCallout = 8
        }

        public static Card RandomCard()
        {
            Card card = new Card();
            object lockObject = new object();
            int myColor;
            int myCard;
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
                }
            }
            return card;
        }
    }

    public static class AFKtimer
    {
        public static Dictionary<ulong, Timer> playTimers = new Dictionary<ulong, Timer>();

        public static void ResetTimer(ulong server)
        {
            if (!playTimers.ContainsKey(server))
                LoggerService.Log(LogSeverity.Error, "Attempted to reset timer that doesn't exist!");
            else
            {
                playTimers[server].Stop();
                playTimers[server].Start();
            }
        }
        public static void StartTimer(ulong server)
        {
            LoggerService.Log(LogSeverity.Debug, "Starting timer!");
            if (playTimers.ContainsKey(server))
                LoggerService.Log(LogSeverity.Warning, "Attempted to start timer that already existed!");
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
            LoggerService.Log(LogSeverity.Debug, "Timer over!");
            ulong serverID = 0;
            foreach (ulong server in playTimers.Keys)
            {
                Timer timer = (Timer)source;
                if (timer.Equals(playTimers[server]))
                    serverID = server;
            }
            if (serverID == 0)
            {
                LoggerService.Log(LogSeverity.Error, "Couldn't figure out what server timer belonged to!");
                return;
            }
            ulong currentPlayer = await QueueHandlerService.GetCurrentPlayer(serverID);
            await UNODatabaseService.RemoveUser(currentPlayer);
            await QueueHandlerService.DropFrontPlayer(serverID);
            LoggerService.Log(LogSeverity.Debug, "SayPlayer");
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
                    LoggerService.Log(LogSeverity.Warning, "Attempted to dispose a timer that was already disposed!");
                else
                    playTimers[server].Dispose();
            }
            playTimers.Remove(server);
        }
    }
}