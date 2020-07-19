using System.Linq;
using System.Threading.Tasks;
using ConnectBot.Templates;
using Dapper;
using Discord;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using UNObot;
using UNObot.Plugins.Helpers;
using Game = ConnectBot.Templates.Game;

namespace ConnectBot.Services
{
    public class DatabaseService
    {
        private readonly LoggerService _logger;
        private readonly Board _defaultBoard = new Board(7, 6);
        private readonly GameQueue _defaultQueue = new GameQueue();
        
        internal string ConnString { get; }

        public DatabaseService(IConfiguration config, LoggerService logger)
        {
            _logger = logger;
            ConnString = config.GetConnectionString();
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            Reset();
        }
        
        private void Reset()
        {
            const string commandText =
                "SET SQL_SAFE_UPDATES = 0; UPDATE UNObot.ConnectBot_Games SET inGame = 0, board = @Board, queue = @Queue, description = null; SET SQL_SAFE_UPDATES = 1;";

            using var db = new MySqlConnection(ConnString);
            try
            {
                db.Execute(commandText, new { Board = _defaultBoard, Queue = _defaultQueue });
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        internal async Task ResetGame(ulong server)
        {
            const string commandText =
                "UPDATE UNObot.ConnectBot_Games SET inGame = 0, board = @Board, queue = @Queue, description = null WHERE server = @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new { Board = _defaultBoard, Queue = _defaultQueue, Server = server });
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        internal async Task<Game> GetGame(ulong server)
        {
            const string commandText = "SELECT * FROM UNObot.ConnectBot_Games WHERE server = @Server";
            await using var db = new MySqlConnection(ConnString);
            try
            {
                var result = await db.QueryAsync<Game>(commandText, new { Server = server });
                var games = result as Game[] ?? result.ToArray();
                if (games.Length > 0)
                    return games[0];
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            await AddGame(server);
            return new Game();
        }

        internal async Task UpdateGame(Game game)
        {
            const string commandText = "UPDATE UNObot.ConnectBot_Games SET board = @Board, `description` = @Description, queue = @Queue WHERE server = @Server";
            await using var db = new MySqlConnection(ConnString);
            try
            {
                lock (game)
                {
                    db.Execute(commandText, new
                    {
                        Board = JsonConvert.SerializeObject(game.Board),
                        game.Description,
                        Queue = JsonConvert.SerializeObject(game.Queue)
                    });
                }
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        internal async Task AddGame(ulong server)
        {
            const string commandText = "INSERT IGNORE UNObot.ConnectBot_Games (server) VALUES @Server";

            await using var db = new MySqlConnection(ConnString);
            try
            {
                await db.ExecuteAsync(commandText, new { Server = server });
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
    }
}