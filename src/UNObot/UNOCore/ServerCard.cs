namespace UNObot.UNOCore
{
    internal class ServerCard
    {
        internal Card Card;
        internal int CardsAllocated = 1;
        internal int CardsAvailable = 1;

        internal ServerCard(Card card)
        {
            Card = card;
        }

        internal bool Equals(ServerCard other)
        {
            return Card.Equals(other.Card);
        }
    }
}