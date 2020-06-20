namespace UNObot.UNOCore
{
    internal class ServerCard
    {
        public Card Card;
        public int CardsAllocated = 1;
        public int CardsAvailable = 1;

        public ServerCard(Card card)
        {
            Card = card;
        }

        public bool Equals(ServerCard other)
        {
            return Card.Equals(other.Card);
        }
    }
}