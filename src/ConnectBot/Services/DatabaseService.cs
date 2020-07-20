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
        private static readonly JsonSerializerSettings JsonSettings;
        private static readonly string DefaultBoard;
        private static readonly string DefaultQueue;

        internal string ConnString { get; }

        static DatabaseService()
        {
            JsonSettings = new JsonSerializerSettings 
            { 
                TypeNameHandling = TypeNameHandling.All
            };
            DefaultBoard = JsonConvert.SerializeObject(new Board());
            DefaultQueue = JsonConvert.SerializeObject(new GameQueue(), JsonSettings);
        }

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
                "SET SQL_SAFE_UPDATES = 0; UPDATE UNObot.ConnectBot_Games SET board = @Board, queue = @Queue, description = NULL, lastChannel = NULL, lastMessage = NULL; SET SQL_SAFE_UPDATES = 1;";

            using var db = new MySqlConnection(ConnString);
            try
            {
                db.Execute(commandText, new { Board = DefaultBoard, Queue = DefaultQueue });
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }
        }
        
        internal async Task ResetGame(ulong server)
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
        
        internal async Task<Game> GetGame(ulong server)
        {
            const string commandText = "SELECT * FROM UNObot.ConnectBot_Games WHERE server = @Server";
            await using var db = new MySqlConnection(ConnString);
            try
            {
                var result = (await db.QueryAsync(commandText, new {Server = server})).ToArray();
                if (result.Length != 0)
                    return new Game(server)
                    {
                        Board = JsonConvert.DeserializeObject<Board>(result[0].board),
                        Queue = JsonConvert.DeserializeObject<GameQueue>(result[0].queue),
                        Description = result[0].description,
                        LastChannel = result[0].lastChannel,
                        LastMessage = result[0].lastMessage
                    };
            }
            catch (MySqlException ex)
            {
                _logger.Log(LogSeverity.Error, "A MySQL error has occurred.", ex);
            }

            await AddGame(server);
            return new Game(server);
        }

        internal async Task UpdateGame(Game game)
        {
            const string commandText = "UPDATE UNObot.ConnectBot_Games SET board = @Board, `description` = @Description, queue = @Queue, lastChannel = @LastChannel, lastMessage = @LastMessage WHERE server = @Server";
            await using var db = new MySqlConnection(ConnString);
            try
            {
                lock (game)
                {
                    db.Execute(commandText, new
                    {
                        game.Server,
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
    }
}