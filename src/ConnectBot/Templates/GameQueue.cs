using System.Collections.Generic;
using Newtonsoft.Json;
using UNObot.Plugins.TerminalCore;

namespace ConnectBot.Templates
{
    public class GameQueue
    {
        [JsonProperty]
        private List<ulong> _players;

        [JsonIgnore] public IReadOnlyList<ulong> Players => _players;

        [JsonProperty]
        [JsonConverter(typeof(IndexedDictionaryConverter))]
        private IndexedDictionary<ulong, int> _inGame;
        
        [JsonIgnore] public IReadOnlyIndexedDictionary<ulong, int> InGame => _inGame;

        public GameQueue()
        {
            _players = new List<ulong>();
            _inGame = new IndexedDictionary<ulong, int>();
        }

        public bool AddPlayer(ulong player)
        {
            if (_players.Contains(player))
                return false;
            _players.Add(player);
            return true;
        }

        public bool RemovePlayer(ulong player)
        {
            // Prevent short-circuiting.
            var a = _players.Remove(player);
            var b = _inGame.Remove(player);
            return a || b;
        }

        public bool GameStarted()
        {
            return _inGame.Count > 0;
        }

        /// <summary>
        /// Shifts the players waiting into the players in-game.
        /// </summary>
        /// <param name="count">How many players to migrate.</param>
        /// <returns>Whether if the action is successful (enough players)</returns>
        public bool Start(int count = 2)
        {
            _inGame.Clear();
            if (_players.Count < count)
                return false;
            // Start at 1, because 0 is "empty".
            for (var i = 1; i <= count; i++)
            {
                _inGame.Add(_players[0], i);
                _players.RemoveAt(0);
            }
            _players.Shuffle();

            return true;
        }

        public (ulong Player, int Color) Next()
        {
            var temp = _inGame[0];
            _inGame.RemoveAt(0);
            _inGame.Add(temp);
            return CurrentPlayer();
        }

        public (ulong Player, int Color) CurrentPlayer()
        {
            return _inGame[0];
        }
    }
}