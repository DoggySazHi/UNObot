using System.Collections.Generic;
using UNObot.Plugins.TerminalCore;

namespace UNObot.UNOCore
{
    internal class ServerDeck
    {
        private static readonly List<string> Colors = new List<string> {"Red", "Green", "Blue", "Yellow"};

        private static readonly List<string> Values = new List<string>
            {"1", "2", "3", "4", "5", "6", "7", "8", "9", "Skip", "Reverse", "+2"};

        private readonly List<ServerCard> _cards;

        internal ServerDeck()
        {
            _cards = new List<ServerCard>();
            //smells like spaghetti code
            for (var i = 0; i < 2; i++)
                foreach (var color in Colors)
                foreach (var value in Values)
                    _cards.Add(new ServerCard(new Card(color, value)));
            foreach (var color in Colors)
                _cards.Add(new ServerCard(new Card(color, "0")));
            for (var i = 0; i < 4; i++)
            {
                _cards.Add(new ServerCard(new Card("Wild", "Color")));
                _cards.Add(new ServerCard(new Card("Wild", "+4")));
            }
        }

        internal void Shuffle()
        {
            _cards.Shuffle();
        }
    }
}