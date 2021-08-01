using System;
using System.Data.Common;
using System.Threading.Tasks;
using ConnectBot.Templates;
using Dapper;
using Discord;
using Newtonsoft.Json;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;
using Game = ConnectBot.Templates.Game;

namespace ConnectBot.Services
{
    public class DatabaseService
    {
        private readonly ILogger _logger;
        private readonly ConnectBotConfig _config;
        private static readonly JsonSerializerSettings JsonSettings;
        private static readonly string DefaultBoard;
        private static readonly string DefaultQueue;
        
        static DatabaseService()
        {
            JsonSettings = new JsonSerializerSettings 
            { 
                TypeNameHandling = TypeNameHandling.All
            };
            DefaultBoard = JsonConvert.SerializeObject(new Board());
            DefaultQueue = JsonConvert.SerializeObject(new GameQueue(), JsonSettings);
        }

        public DatabaseService(ILogger logger, ConnectBotConfig config)
        {
            _logger = logger;
            _config = config;
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        public async Task ResetGame(ulong server)
        {
            const string commandText =
                "UPDATE ConnectBot.Games SET board = @Board, queue = @Queue, description = null WHERE server = @Server";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { Board = DefaultBoard, Queue = DefaultQueue, Server = Convert.ToDecimal(server) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
            }
        }
        
        public async Task<Game> GetGame(ulong server)
        {
            const string commandText = "SELECT * FROM ConnectBot.Games WHERE server = @Server";
            await using var db = _config.GetConnection();
            try
            {
                var result = await db.QueryFirstOrDefaultAsync(_config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server) });
                if (!ReferenceEquals(null, result))
                    return new Game(server)
                    {
                        GameMode = (GameMode) result.gameMode,
                        Board = JsonConvert.DeserializeObject<Board>(result.board),
                        Queue = JsonConvert.DeserializeObject<GameQueue>(result.queue),
                        Description = result.description,
                        LastChannel = Convert.ToUInt64(result.lastChannel),
                        LastMessage = Convert.ToUInt64(result.lastMessage)
                    };
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
            }

            await AddGame(server);
            return new Game(server);
        }

        public async Task UpdateGame(Game game)
        {
            const string commandText = "UPDATE ConnectBot.Games SET gameMode = @GameMode, board = @Board, `description` = @Description, queue = @Queue, lastChannel = @LastChannel, lastMessage = @LastMessage WHERE server = @Server";
            await using var db = _config.GetConnection();
            try
            {
                lock (game)
                {
                    db.Execute(_config.ConvertSql(commandText), new
                    {
                        Server = Convert.ToDecimal(game.Server),
                        GameMode = (byte) game.GameMode,
                        Board = JsonConvert.SerializeObject(game.Board),
                        game.Description,
                        Queue = JsonConvert.SerializeObject(game.Queue, JsonSettings),
                        LastChannel = Convert.ToDecimal(game.LastChannel),
                        LastMessage = Convert.ToDecimal(game.LastMessage)
                    });
                }
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
            }
        }
        
        private async Task AddGame(ulong server)
        {
            const string commandText = "INSERT IGNORE ConnectBot.Games (server, board, queue) VALUES (@Server, @Board, @Queue)";

            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { Server = Convert.ToDecimal(server), Board = DefaultBoard, Queue = DefaultQueue });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
            }
        }
        
        public async Task AddUser(ulong user)
        {
            const string commandText =
                "INSERT IGNORE INTO ConnectBot.Players (userid) VALUES(@User)";
            
            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { User = Convert.ToDecimal(user) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
            }
        }
        
        public enum ConnectBotStat { GamesJoined, GamesPlayed, GamesWon }

        public async Task<(int GamesJoined, int GamesPlayed, int GamesWon)> GetStats(ulong user)
        {
            const string commandText =
                "SELECT gamesJoined, gamesPlayed, gamesWon FROM ConnectBot.Players WHERE userid = @User";
            
            await using var db = _config.GetConnection();
            try
            {
                var results = await db.QueryFirstOrDefaultAsync(_config.ConvertSql(commandText), new { User = Convert.ToDecimal(user) });
                if (!ReferenceEquals(null, results))
                    return (results.gamesJoined, results.gamesPlayed, results.gamesWon);
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
            }

            return (0, 0, 0);
        }
        
        public async Task UpdateStats(ulong user, ConnectBotStat stat)
        {
            var enumStr = stat.ToString();
            var column = enumStr[..1].ToLower() + enumStr[1..];
            var commandText =
                $"UPDATE ConnectBot.Players SET {column} = {column} + 1 WHERE userid = @User";
            
            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { User = Convert.ToDecimal(user) });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
            }
        }
        
        public async Task<(int DefaultWidth, int DefaultHeight, int DefaultConnect)> GetDefaultBoardDimensions(ulong user)
        {
            const string commandText =
                "SELECT defaultWidth, defaultHeight, defaultConnect FROM ConnectBot.Players WHERE userid = @User";
            
            await using var db = _config.GetConnection();
            try
            {
                var results = await db.QueryFirstOrDefaultAsync(_config.ConvertSql(commandText), new { User = Convert.ToDecimal(user) });
                if (!ReferenceEquals(null, results))
                    return (results.defaultWidth, results.defaultHeight, results.defaultConnect);
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
            }

            return (7, 6, 4);
        }
        
        public async Task SetDefaultBoardDimensions(ulong user, int width, int height, int connect)
        {
            const string commandText = "UPDATE ConnectBot.Players SET defaultWidth = @Width, defaultHeight = @Height, defaultConnect = @Connect WHERE userid = @User";
            
            await using var db = _config.GetConnection();
            try
            {
                await db.ExecuteAsync(_config.ConvertSql(commandText), new { User = Convert.ToDecimal(user), Width = width, Height = height, Connect = connect });
            }
            catch (DbException ex)
            {
                _logger.Log(LogSeverity.Error, "A SQL error has occurred.", ex);
            }
        }
    }
}