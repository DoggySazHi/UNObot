using Dapper.Contrib.Extensions;

namespace ConnectBot.Templates
{
    public class Game
    {
        [Key]
        public ulong Server { get; set; }
        public Board Board { get; set; }
        public GameQueue Queue { get; set; }
        public string Description { get; set; }
        public ulong? LastChannel { get; set; }
        public ulong? LastMessage { get; set; }
    }
}