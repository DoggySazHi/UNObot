using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Discord;
using Discord.WebSocket;
using DuplicateDetector.Templates;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;

namespace DuplicateDetector.Services
{
    public class IndexerService
    {
        private readonly ILogger _logger;
        private readonly DiscordSocketClient _client;
        private readonly IAIConfig _config;

        private readonly string _cacheDir;
        private readonly string _imageDir;
        
        public IndexerService(ILogger logger, DiscordSocketClient client, IAIConfig config)
        {
            _logger = logger;
            _client = client;
            _config = config;
            _client.MessageReceived += AddImage;
            
            var pluginDir = PluginHelper.Directory();
            _cacheDir = Path.Combine(pluginDir, "indexes");
            _imageDir = Path.Combine(pluginDir, "images");
            
            Directory.CreateDirectory(_cacheDir);
            Directory.CreateDirectory(_imageDir);
        }

        private Task AddImage(SocketMessage message)
        {
            Task.Run(async () =>
            {
                if (!await AutoLog(message.Channel.Id))
                    return;
                UploadImages(message);
            });
            return Task.CompletedTask;
        }

        private void UploadImages(IMessage message)
        {
            Task.Run(async () =>
            {
                await using var db = new MySqlConnection(_config.DBConnStr);

                foreach (var attachment in message.Attachments)
                {
#pragma warning disable 4014
                    db.ExecuteAsync(
                        "INSERT INTO DuplicateDetector.Images (channel, message, author, url, proxy_url, spoiler, posted) VALUES (@Channel, @Message, @Author, @URL, @Proxy_URL, @Spoiler, @Posted)",
                        new 
                        {
                            Channel = message.Channel.Id,
                            Author = message.Author.Id,
                            Message = message.Id,
                            URL = attachment.Url,
                            Proxy_URL = attachment.ProxyUrl,
                            Spoiler = attachment.IsSpoiler(),
                            Posted = message.Timestamp.UtcDateTime
                        });
#pragma warning restore 4014
                }
            });
        }

        private async Task<bool> AutoLog(ulong channel)
        {
            try
            {
                await using var db = new MySqlConnection(_config.DBConnStr);
                
                var output = await db.QueryAsync<bool>(
                    "SELECT autolog FROM DuplicateDetector.Channels WHERE channel = @Channel",
                    new {Channel = channel});
                
                return output.SingleOrDefault();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task Index(ITextChannel channel)
        {
            try
            { 
                var counter = 0;
                var path = Path.Combine(_cacheDir, $"url_list-{channel.Id}.json");
                await using var sw = new StreamWriter(path);
                await using var db = new MySqlConnection(_config.DBConnStr);
                _ = db.ExecuteAsync(
                    "INSERT INTO DuplicateDetector.Channels (channel, server, autolog) VALUES (@Channel, @Server, 1) ON DUPLICATE KEY UPDATE autolog = 1",
                    new { Channel = channel.Id, Server = channel.Guild.Id });
                var writeMessages = new List<ImageMessage>();
                _logger.Log(LogSeverity.Debug, "Starting indexer.");
                await foreach (var messages in channel.GetMessagesAsync(100000))
                {
                    foreach (var message in messages)
                    {
                        var tempMessage = new ImageMessage
                        {
                            Author = message.Author.Id,
                            Link = message.GetJumpUrl(),
                            Attachments = new List<ImageAttachment>()
                        };
                        
                        foreach(var attachment in message.Attachments)
                            tempMessage.Attachments.Add(
                                new ImageAttachment { URL = attachment.Url, ProxyURL = attachment.ProxyUrl, IsSpoiler = attachment.IsSpoiler()});

                        UploadImages(message);
                        
                        if(tempMessage.Attachments.Count > 0)
                            writeMessages.Add(tempMessage);
                        
                        counter += tempMessage.Attachments.Count;
                    }
                    _logger.Log(LogSeverity.Debug, $"Processed {counter} images!");
                    await Task.Delay(1000);
                }
                _logger.Log(LogSeverity.Debug, "Serializing...");
                var text = JsonConvert.SerializeObject(writeMessages);
                _logger.Log(LogSeverity.Debug, "Finished!");
                await sw.WriteAsync(text);
            }
            catch (Exception e)
            {
                _logger.Log(LogSeverity.Error, "crap.", e);
                throw;
            }
        }

        public async Task Index(ulong guild, ulong channel)
            => await Index(_client.GetGuild(guild).GetTextChannel(channel));

        public async Task Download()
        {
            const string command = "SELECT id, url, proxy_url FROM DuplicateDetector.Images";
            await using var connection = new MySqlConnection(_config.DBConnStr);
            try
            {
                connection.Open();
                await using var mySQLCmd = new MySqlCommand(command, connection);
                await using var reader = await mySQLCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var url = reader.GetString(1) ?? reader.GetString(2);
#pragma warning disable 4014
                    DownloadImage(url, reader.GetInt32(0));
#pragma warning restore 4014
                }
            }
            catch (Exception e)
            {
                _logger.Log(LogSeverity.Error, "Downloader failed!", e);
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        
        private volatile int _tasks = 100;

        private async Task DownloadImage(string url, int name, bool overwrite = false)
        {
            try
            {
                while(_tasks <= 0) {}
                _tasks--;
                var extension = url.Split("/")[^1].Split(".")[^1];
                var path = Path.Combine(_imageDir, $"{name}.{extension}");
                var uri = new Uri(url);
                var client = new HttpClient();
                var response = await client.GetAsync(uri);
                if (File.Exists(path))
                    if (overwrite)
                        File.Delete(path);
                    else
                        return;
                await using var fs = new FileStream(path, FileMode.CreateNew);
                await response.Content.CopyToAsync(fs);
            }
            catch (Exception e)
            {
                _logger.Log(LogSeverity.Error, "Failed to download file!", e);
                throw;
            }
            finally
            {
                _tasks++;
            }
        }
    }
}