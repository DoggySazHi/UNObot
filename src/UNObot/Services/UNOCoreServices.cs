using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using Discord;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace UNObot.Services
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HelpAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:UNObot.Services.HelpAttribute" /> class.
        /// </summary>
        /// <param name="usages">Usages of the command.</param>
        /// <param name="helpMsg">HelpAttribute message.</param>
        /// <param name="active">Check if command should be displayed in the help list.</param>
        /// <param name="version">Version when the command was first introduced.</param>
        [JsonConstructor]
        public HelpAttribute(string[] usages, string helpMsg, bool active, string version)
        {
            this.Usages = usages;
            this.HelpMsg = helpMsg;
            this.Active = active;
            this.Version = version;
        }

        public string[] Usages { get; }
        public string HelpMsg { get; }
        public bool Active { get; }
        public string Version { get; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DisableDMsAttribute : Attribute
    {
        [JsonConstructor]
        public DisableDMsAttribute()
        {
            Disabled = true;
        }

        [JsonConstructor]
        public DisableDMsAttribute(bool disabled)
        {
            this.Disabled = disabled;
        }

        public bool Disabled { get; }

        public bool Enabled => !Disabled;
    }

    public class Command
    {
        [JsonConstructor]
        public Command(string commandName, List<string> aliases, List<string> usages, string help, bool active,
            string version)
        {
            this.CommandName = commandName;
            this.Aliases = aliases;
            this.Usages = usages;
            this.Help = help;
            this.Active = active;
            this.Version = version;
        }

        [JsonConstructor]
        public Command(string commandName, List<string> aliases, List<string> usages, string help, bool active,
            string version, bool disableDMs) : this(commandName, aliases, usages, help, active, version)
        {
            this.DisableDMs = disableDMs;
        }

        public string CommandName { get; set; }
        public List<string> Usages { get; set; }
        public List<string> Aliases { get; set; }
        public string Help { get; set; }
        public bool Active { get; set; }
        public string Version { get; set; }
        public bool DisableDMs { get; set; }
    }

    public class Card
    {
        public string Color;
        public string Value;

        public override string ToString()
        {
            return $"{Color} {Value}";
        }

        public bool Equals(Card other)
        {
            return Value == other.Value && Color == other.Color;
        }
    }

    public class ServerCard
    {
        public Card Card;
        public int CardsAllocated = 1;
        public int CardsAvailable = 1;

        public ServerCard(Card card)
        {
            this.Card = card;
        }

        public bool Equals(ServerCard other)
        {
            return Card.Equals(other.Card);
        }
    }

    public class ServerDeck
    {
        public static readonly List<string> Colors = new List<string> {"Red", "Green", "Blue", "Yellow"};

        public static readonly List<string> Values = new List<string>
            {"1", "2", "3", "4", "5", "6", "7", "8", "9", "Skip", "Reverse", "+2"};

        public List<ServerCard> Cards;

        public ServerDeck()
        {
            //smells like spaghetti code
            for (var i = 0; i < 2; i++)
                foreach (var color in Colors)
                foreach (var value in Values)
                    Cards.Add(new ServerCard(new Card
                    {
                        Color = color,
                        Value = value
                    }));
            foreach (var color in Colors)
                Cards.Add(new ServerCard(new Card
                {
                    Color = color,
                    Value = "0"
                }));
            for (var i = 0; i < 4; i++)
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
        [ThreadStatic] private static Random _local;

        public static Random ThisThreadsRandom =>
            _local ?? (_local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId)));

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = ThisThreadsRandom.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public static class UNOCoreServices
    {
        [Flags]
        public enum GameMode
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

        public static Card RandomCard()
        {
            var card = new Card();
            var lockObject = new object();
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
                _ => "Blue"
            };
            if (myCard < 10)
            {
                card.Value = myCard.ToString();
            }
            else
            {
                //4 is wild, 1-3 is action
                var action = ThreadSafeRandom.ThisThreadsRandom.Next(1, 5);
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
                        var wild = ThreadSafeRandom.ThisThreadsRandom.Next(1, 3);
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

    public static class AfKtimer
    {
        public static Dictionary<ulong, Timer> PlayTimers = new Dictionary<ulong, Timer>();

        public static void ResetTimer(ulong server)
        {
            if (!PlayTimers.ContainsKey(server))
            {
                LoggerService.Log(LogSeverity.Error, "Attempted to reset timer that doesn't exist!");
            }
            else
            {
                PlayTimers[server].Stop();
                PlayTimers[server].Start();
            }
        }

        public static void StartTimer(ulong server)
        {
            LoggerService.Log(LogSeverity.Debug, "Starting timer!");
            if (PlayTimers.ContainsKey(server))
            {
                LoggerService.Log(LogSeverity.Warning, "Attempted to start timer that already existed!");
            }
            else
            {
                PlayTimers[server] = new Timer
                {
                    Interval = 90000,
                    AutoReset = false
                };
                PlayTimers[server].Elapsed += TimerOver;
                PlayTimers[server].Start();
            }
        }

        private static async void TimerOver(object source, ElapsedEventArgs e)
        {
            LoggerService.Log(LogSeverity.Debug, "Timer over!");
            ulong serverId = 0;
            foreach (var server in PlayTimers.Keys)
            {
                var timer = (Timer) source;
                if (timer.Equals(PlayTimers[server]))
                    serverId = server;
            }

            if (serverId == 0)
            {
                LoggerService.Log(LogSeverity.Error, "Couldn't figure out what server timer belonged to!");
                return;
            }

            var currentPlayer = await QueueHandlerService.GetCurrentPlayer(serverId);
            await UNODatabaseService.RemoveUser(currentPlayer);
            await QueueHandlerService.DropFrontPlayer(serverId);
            LoggerService.Log(LogSeverity.Debug, "SayPlayer");
            await Program.SendMessage($"<@{currentPlayer}>, you have been AFK removed.\n", serverId);
            await Program.SendPM("You have been AFK removed.", currentPlayer);
            if (await QueueHandlerService.PlayerCount(serverId) == 0)
            {
                await UNODatabaseService.ResetGame(serverId);
                await Program.SendMessage("Game has been reset, due to nobody in-game.", serverId);
                DeleteTimer(serverId);
                return;
            }

            ResetTimer(serverId);
            await Program.SendMessage($"It is now <@{await QueueHandlerService.GetCurrentPlayer(serverId)}> turn.\n",
                serverId);
        }

        public static void DeleteTimer(ulong server)
        {
            if (PlayTimers.ContainsKey(server))
            {
                if (PlayTimers[server] == null)
                    LoggerService.Log(LogSeverity.Warning, "Attempted to dispose a timer that was already disposed!");
                else
                    PlayTimers[server].Dispose();
            }

            PlayTimers.Remove(server);
        }
    }
}