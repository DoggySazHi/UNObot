using UNObot.TerminalCore;

namespace UNObot.UNOCore
{
    internal class Card
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

        static Card RandomCard()
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
        
        static Card[] RandomCard(int count)
        {
            var cards = new Card[count];
            for (var i = 0; i < count; i++)
                cards[i] = RandomCard();
            return cards;
        }
    }
}