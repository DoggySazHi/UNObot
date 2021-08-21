using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Dapper;
using Discord;
using Discord.WebSocket;
using DuplicateDetector.Templates;
using Newtonsoft.Json;
using UNObot.Plugins;
using UNObot.Plugins.Helpers;
using UNObot.Plugins.Settings;

namespace DuplicateDetector.Services
{
    public class IndexerService
    {
        private readonly ILogger _logger;
        private readonly DiscordSocketClient _client;
        private readonly DuplicateDetectorConfig _config;

        private readonly string _cacheDir;
        private readonly string _imageDir;
        
        public IndexerService(ILogger logger, DiscordSocketClient client, DuplicateDetectorConfig config)
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
            
            var settings = new Setting("DuplicateDetector Settings");
            settings.UpdateSetting("Watch Channels", new ChannelIDList());
            SettingsManager.RegisterSettings("DuplicateDetector", settings);
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
                await using var db = _config.GetConnection();

                foreach (var attachment in message.Attachments)
                {
                    try
                    {
                        await db.ExecuteAsync(
                            _config.ConvertSql(
                                "INSERT INTO DuplicateDetector.Images (channel, message, author, url, proxy_url, spoiler, posted) VALUES (@Channel, @Message, @Author, @URL, @Proxy_URL, @Spoiler, @Posted)"),
                            new
                            {
                                Channel = Convert.ToDecimal(message.Channel.Id),
                                Author = Convert.ToDecimal(message.Author.Id),
                                Message = Convert.ToDecimal(message.Id),
                                URL = attachment.Url,
                                Proxy_URL = attachment.ProxyUrl,
                                Spoiler = attachment.IsSpoiler(),
                                Posted = message.Timestamp.UtcDateTime
                            });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                }
            });
        }

        private async Task<bool> AutoLog(ulong channel)
        {
            try
            {
                await using var db = _config.GetConnection();
                
                return await db.ExecuteScalarAsync<bool>(
                    _config.ConvertSql("SELECT autolog FROM DuplicateDetector.Channels WHERE channel = @Channel"),
                    new { Channel = Convert.ToDecimal(channel) });
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
                await using var db = _config.GetConnection();
                _ = db.ExecuteAsync(
                    _config.ConvertSql(
                        "INSERT INTO DuplicateDetector.Channels (channel, server, autolog) VALUES (@Channel, @Server, 1) ON DUPLICATE KEY UPDATE autolog = 1"
                        ),new { Channel = Convert.ToDecimal(channel.Id), Server = Convert.ToDecimal(channel.Guild.Id) });
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

        private class Image
        {
            public Image(int id, string url)
            {
                ID = id;
                URL = url;
            }

            public int ID { get; }
            public string URL { get; }
        }
        
        public async Task Download()
        {
            const string command = "SELECT id, url FROM DuplicateDetector.Images";
            await using var db = _config.GetConnection();
            try
            {
                await using var query = await db.ExecuteReaderAsync(_config.ConvertSql(command));
                var rowParser = query.GetRowParser<Image>();
                
                while (await query.ReadAsync())
                {
                    var data = rowParser(query);
#pragma warning disable 4014
                    DownloadImage(data.URL, data.ID);
#pragma warning restore 4014
                }
            }
            catch (Exception e)
            {
                _logger.Log(LogSeverity.Error, "Downloader failed!", e);
                throw;
            }
        }
        
        private int _tasks = 100;

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