using System.Threading.Tasks;
using ConnectBot.Templates;
using Dapper;
using Discord;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;
using Game = ConnectBot.Templates.Game;

namespace ConnectBot.Services
{
    public class DatabaseService
    {
        private readonly ILogger _logger;
        private static readonly JsonSerializerSettings JsonSettings;
        private static readonly string DefaultBoard;
        private static readonly string DefaultQueue;

        public string ConnString { get; }

        static DatabaseService()
        {
            JsonSettings = new JsonSerializerSettings 
            { 
                TypeNameHandling = TypeNameHandling.All
            };
            DefaultBoard = JsonConvert.SerializeObject(new Board());
            DefaultQueue = JsonConvert.SerializeObject(new GameQueue(), JsonSettings);
        }

        public DatabaseService(IConfig config, ILogger logger)
        {
            _logger = logger;
            ConnString = config.GetConnectionString();
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        public async Task ResetGame(ulong server)
        {
            const string commandText =
                "UPDATE UNObot.ConnectBot_Games SET board = @Board, queue = @Queue, description = null WHERE server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new { Board = DefaultBoard, Queue = DefaultQueue, Server = server });
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        public async Task<Game> GetGame(ulong server)
        {
            const string commandText = "SELECT * FROM UNObot.ConnectBot_Games WHERE server = @Server";
            await using var db = new MySqlConnection(ConnString);
            try
            {
                var result = await db.QueryFirstOrDefaultAsync(commandText, new {Server = server});
                if (!ReferenceEquals(null, result))
                    return new Game(server)
                    {
                        GameMode = (GameMode) result.gameMode,
                        Board = JsonConvert.DeserializeObject<Board>(result.board),
                        Queue = JsonConvert.DeserializeObject<GameQueue>(result.queue),
                        Description = result.description,
                        LastChannel = result.lastChannel,
                        LastMessage = result.lastMessage
                    };
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            await AddGame(server);
            return new Game(server);
        }

        public async Task UpdateGame(Game game)
        {
            const string commandText = "UPDATE UNObot.ConnectBot_Games SET gameMode = @GameMode, board = @Board, `description` = @Description, queue = @Queue, lastChannel = @LastChannel, lastMessage = @LastMessage WHERE server = @Server";
            await using var db = new MySqlConnection(ConnString);
            try
            {
                lock (game)
                {
                    db.Execute(commandText, new
                    {
                        game.Server,
                        GameMode = (ushort) game.GameMode,
                        Board = JsonConvert.SerializeObject(game.Board),
                        game.Description,
                        Queue = JsonConvert.SerializeObject(game.Queue, JsonSettings),
                        game.LastChannel,
                        game.LastMessage
                    });
                }
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        private async Task AddGame(ulong server)
        {
            const string commandText = "INSERT IGNORE UNObot.ConnectBot_Games (server, board, queue) VALUES (@Server, @Board, @Queue)";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new { Server = server, Board = DefaultBoard, Queue = DefaultQueue });
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        public async Task AddUser(ulong user)
        {
            const string commandText =
                "INSERT IGNORE INTO ConnectBot_Players (userid) VALUES(@User)";
            
            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new {User = user});
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        public enum ConnectBotStat { GamesJoined, GamesPlayed, GamesWon }

        public async Task<(int GamesJoined, int GamesPlayed, int GamesWon)> GetStats(ulong user)
        {
            const string commandText =
                "SELECT gamesJoined, gamesPlayed, gamesWon FROM ConnectBot_Players WHERE userid = @User";
            
            await using var db = new MySqlConnection(ConnString);
            try
            {
                var results = await db.QueryFirstOrDefaultAsync(commandText, new { User = user });
                if (!ReferenceEquals(null, results))
                    return (results.gamesJoined, results.gamesPlayed, results.gamesWon);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return (0, 0, 0);
        }
        
        public async Task UpdateStats(ulong user, ConnectBotStat stat)
        {
            var enumStr = stat.ToString();
            var column = enumStr.Substring(0, 1).ToLower() + enumStr.Substring(1);
            var commandText =
                $"UPDATE UNObot.ConnectBot_Players SET {column} = {column} + 1 WHERE userid = @User";
            
            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new { User = user });
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        public async Task<(int DefaultWidth, int DefaultHeight, int DefaultConnect)> GetDefaultBoardDimensions(ulong user)
        {
            const string commandText =
                "SELECT defaultWidth, defaultHeight, defaultConnect FROM ConnectBot_Players WHERE userid = @User";
            
            await using var db = new MySqlConnection(ConnString);
            try
            {
                var results = await db.QueryFirstOrDefaultAsync(commandText, new { User = user });
                if (!ReferenceEquals(null, results))
                    return (results.defaultWidth, results.defaultHeight, results.defaultConnect);
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            return (7, 6, 4);
        }
        
        public async Task SetDefaultBoardDimensions(ulong user, int width, int height, int connect)
        {
            const string commandText = "UPDATE ConnectBot_Players SET defaultWidth = @Width, defaultHeight = @Height, defaultConnect = @Connect WHERE userid = @User";
            
            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new { User = user, Width = width, Height = height, Connect = connect });
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
    }
}