using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Discord;
using Discord.WebSocket;
using DuplicateDetector.Templates;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _config;

        private readonly string _cacheDir;
        private readonly string _imageDir;
        
        public IndexerService(ILogger logger, DiscordSocketClient client, IConfiguration config)
        {
            _logger = logger;
            _client = client;
            _config = config;
            _client.MessageReceived += AddImage;
            
            var pluginDir = Path.Join(PluginHelper.Directory(), "DuplicateDetector");
            _cacheDir = Path.Join(pluginDir, "indexes");
            _imageDir = Path.Join(pluginDir, "images");
            
            Directory.CreateDirectory(pluginDir);
            Directory.CreateDirectory(_cacheDir);
            Directory.CreateDirectory(_imageDir);
        }

        private async Task AddImage(SocketMessage message)
        {
            await using var db = new MySqlConnection(_config.GetConnectionString());
            foreach (var attachment in message.Attachments)
            {
#pragma warning disable 4014
                db.ExecuteAsync(
                    "INSERT INTO DuplicateDetector.Images (author, channel, message, url, proxy_url, spoiler, posted) VALUES (@author, @message, @url, @proxy_url, @spoiler, @posted)",
                    new 
                    {
                        author = message.Author.Id,
                        message = message.GetJumpUrl(),
                        url = attachment.Url,
                        proxy_url = attachment.ProxyUrl,
                        spoiler = attachment.IsSpoiler(),
                        posted = message.Timestamp.UtcDateTime
                    });
#pragma warning restore 4014
            }
        }

        public async Task Index(ISocketMessageChannel channel)
        {
            try
            { 
                var counter = 0;
                var path = Path.Join(_cacheDir, $"url_list-{channel.Id}.json");
                await using var sw = new StreamWriter(path);
                await using var db = new MySqlConnection(_config.GetConnectionString());
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
                        
                        foreach (var attachment in message.Attachments)
                        {
                            tempMessage.Attachments.Add(new ImageAttachment
                            {
                                URL = attachment.Url,
                                ProxyURL = attachment.ProxyUrl,
                                IsSpoiler = attachment.IsSpoiler()
                            });
                            counter++;
                            
    #pragma warning disable 4014
                            db.ExecuteAsync(
                                "INSERT INTO DuplicateDetector.Images (author, channel, message, url, proxy_url, spoiler, posted) VALUES (@author, @message, @url, @proxy_url, @spoiler, @posted)",
                                new 
                                {
                                    author = tempMessage.Author,
                                    message = tempMessage.Link,
                                    url = attachment.Url,
                                    proxy_url = attachment.ProxyUrl,
                                    spoiler = attachment.IsSpoiler(),
                                    posted = message.Timestamp.UtcDateTime
                                });
    #pragma warning restore 4014
                        }
                        
                        if(tempMessage.Attachments.Count > 0)
                            writeMessages.Add(tempMessage);
                    }
                    _logger.Log(LogSeverity.Debug, $"Processed {counter} images!");
                    await Task.Delay(1000);
                }
                _logger.Log(LogSeverity.Debug, $"Serializing...");
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
            await using var connection = new MySqlConnection(_config.GetConnectionString());
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
                var path = Path.Join(_imageDir, $"{name}.{extension}");
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