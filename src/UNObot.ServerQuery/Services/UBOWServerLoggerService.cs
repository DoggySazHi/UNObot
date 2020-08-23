﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Newtonsoft.Json;
using UNObot.Plugins;

namespace UNObot.ServerQuery.Services
{
    internal class UBOWServerLoggerService
    {
        private const string JsonFileName = "UBOWLog.json";
        private const string FileName = "UBOWLog.serverlog";
        private const string Ip = "108.61.100.48";
        private const ushort QueryPort = 25444 + 1;

        private const int Attempts = 3;

        private readonly Timer _logTimer;
        private ServerLog _logs;

        private readonly ILogger _logger;
        private readonly QueryHandlerService _query;
        
        public bool Ready { get; private set; }

        public UBOWServerLoggerService(ILogger logger, QueryHandlerService query)
        {
            _logger = logger;
            _query = query;
            
            _logger.Log(LogSeverity.Info, "Loading logger service...");
            _logTimer = new Timer
            {
                AutoReset = true,
                Interval = 1000 * 60
            };
            _logTimer.Elapsed += LogMinute;
            Task.Run(ReadLogs);
        }

        private async Task ReadLogs()
        {
            if (!File.Exists(FileName) && File.Exists(JsonFileName))
            {
                _logger.Log(LogSeverity.Info, "Found old JSON file, please wait as we upgrade...");
                string data;
                using (var sr = new StreamReader(JsonFileName))
                {
                    data = await sr.ReadToEndAsync();
                }

                var result = JsonConvert.DeserializeObject(data, typeof(ServerLog));
                if (result is ServerLog logFile)
                {
                    _logs = logFile;
                    await _logs.WriteToFile(FileName);
                    _logger.Log(LogSeverity.Info, "Successfully upgraded file!");
                }
                else
                {
                    _logger.Log(LogSeverity.Error, "Failed to read logs! Created new logging service.");
                    File.Delete(JsonFileName);
                }
            }

            if (!File.Exists(FileName) && !File.Exists(JsonFileName))
            {
                _logger.Log(LogSeverity.Info, "Started new logging service.");
                _logs = new ServerLog(_logger)
                {
                    ListOLogs = new List<Log>()
                };
                await _logs.WriteToFile(FileName);
                _logTimer.Enabled = true;
                return;
            }

            _logs = new ServerLog(_logger);
            await _logs.ReadFromFile(FileName);
            RecalculateValues();
            _logTimer.Enabled = true;
            _logger.Log(LogSeverity.Info, "UBOWS Logger initialized!");
            Ready = true;
        }

        private void LogMinute(object sender, ElapsedEventArgs e)
        {
            Task.Run(async () =>
            {
                var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                byte playerCount = 0;
                var serverUp = false;

                for (var i = 0; i < Attempts; i++)
                {
                    serverUp = _query.GetInfo(Ip, QueryPort, out var output);
                    if (!serverUp) continue;
                    playerCount = output.Players;
                    break;
                }

                var nowLog = new Log
                {
                    Timestamp = timestamp,
                    PlayerCount = playerCount,
                    ServerUp = serverUp
                };
                _logs.ListOLogs.Add(nowLog);
                await _logs.AppendToFile(FileName, nowLog);
                if (_logs.ListOLogs.Count >= 365 * 24 * 60)
                    _logs.ListOLogs.RemoveRange(0, _logs.ListOLogs.Count - 365 * 24 * 60);
                RecalculateValues();
            });
        }

        private void RecalculateValues()
        {
            var timeNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            const long timeHour = 1000 * 60 * 60;
            const long timeDay = timeHour * 24;
            const long timeWeek = timeDay * 7;
            const long timeMonth = timeDay * 30;
            const long timeYear = timeDay * 365;

            var lastHour = _logs.ListOLogs.FindAll(o => o.Timestamp > timeNow - timeHour);
            var lastDay = _logs.ListOLogs.FindAll(o => o.Timestamp > timeNow - timeDay);
            var lastWeek = _logs.ListOLogs.FindAll(o => o.Timestamp > timeNow - timeWeek);
            var lastMonth = _logs.ListOLogs.FindAll(o => o.Timestamp > timeNow - timeMonth);
            var lastYear = _logs.ListOLogs.FindAll(o => o.Timestamp > timeNow - timeYear);

            _logs.AverageLastHour = 1.0f * lastHour.Sum(o => o.PlayerCount) / lastHour.Count;
            _logs.AverageLast24H = 1.0f * lastDay.Sum(o => o.PlayerCount) / lastDay.Count;
            _logs.AverageLastWeek = 1.0f * lastWeek.Sum(o => o.PlayerCount) / lastWeek.Count;
            _logs.AverageLastMonth = 1.0f * lastMonth.Sum(o => o.PlayerCount) / lastMonth.Count;
            _logs.AverageLastYear = 1.0f * lastYear.Sum(o => o.PlayerCount) / lastYear.Count;
        }

        internal ServerAverages GetAverages()
        {
            return new ServerAverages
            {
                AverageLastHour = _logs.AverageLastHour,
                AverageLast24H = _logs.AverageLast24H,
                AverageLastWeek = _logs.AverageLastWeek,
                AverageLastMonth = _logs.AverageLastMonth,
                AverageLastYear = _logs.AverageLastYear,
                Ready = true
            };
        }
    }

    internal struct Log
    {
        internal long Timestamp { get; set; }
        internal byte PlayerCount { get; set; }
        internal bool ServerUp { get; set; }
    }

    internal class ServerAverages
    {
        internal float AverageLastHour { get; set; }
        internal float AverageLast24H { get; set; }
        internal float AverageLastWeek { get; set; }
        internal float AverageLastMonth { get; set; }
        internal float AverageLastYear { get; set; }
        internal bool Ready { get; set; }
    }

    internal class ServerLog
    {
        private readonly ILogger _logger;
        
        internal ServerLog(ILogger logger)
        {
            _logger = logger;
        }

        internal float AverageLastHour { get; set; }
        internal float AverageLast24H { get; set; }
        internal float AverageLastWeek { get; set; }
        internal float AverageLastMonth { get; set; }
        internal float AverageLastYear { get; set; }
        internal List<Log> ListOLogs { get; set; }

        internal async Task WriteToFile(string fileName)
        {
            await using var sw = new StreamWriter(fileName);
            await sw.WriteLineAsync("LH" + AverageLastHour);
            await sw.WriteLineAsync("LD" + AverageLast24H);
            await sw.WriteLineAsync("LW" + AverageLastWeek);
            await sw.WriteLineAsync("LM" + AverageLastMonth);
            await sw.WriteLineAsync("LY" + AverageLastYear);
            foreach (var item in ListOLogs) await AppendToFile(fileName, item, sw);
        }

        internal async Task AppendToFile(string fileName, Log log, StreamWriter sw = null)
        {
            var selfCreated = false;
            if (sw == null)
            {
                sw = new StreamWriter(fileName, true);
                selfCreated = true;
            }

            var sb = new StringBuilder("L", 16);
            sb.Append(log.PlayerCount);
            sb.Append(",");
            sb.Append(log.ServerUp ? 1 : 0);
            sb.Append(",");
            sb.Append(log.Timestamp);
            await sw.WriteLineAsync(sb);

            if (selfCreated)
                await sw.DisposeAsync();
        }

        internal async Task ReadFromFile(string fileName)
        {
            if (ListOLogs == null)
                ListOLogs = new List<Log>();
            else if (ListOLogs.Count != 0)
                ListOLogs.Clear();
            using var sr = new StreamReader(fileName);
            var data = await sr.ReadLineAsync();
            while (data != null)
            {
                try
                {
                    if (data.StartsWith("LH"))
                    {
                        AverageLastHour = float.Parse(data.Substring(2));
                    }
                    else if (data.StartsWith("LD"))
                    {
                        AverageLast24H = float.Parse(data.Substring(2));
                    }
                    else if (data.StartsWith("LW"))
                    {
                        AverageLastWeek = float.Parse(data.Substring(2));
                    }
                    else if (data.StartsWith("LM"))
                    {
                        AverageLastMonth = float.Parse(data.Substring(2));
                    }
                    else if (data.StartsWith("LY"))
                    {
                        AverageLastYear = float.Parse(data.Substring(2));
                    }
                    else if (data.StartsWith("L"))
                    {
                        var split = data.Substring(1).Split(",");
                        if (split.Length != 3)
                        {
                            _logger.Log(LogSeverity.Warning, $"Invalid record in file! Read {data}.");
                            continue;
                        }

                        var playerCount = byte.Parse(split[0]);
                        var serverUp = int.Parse(split[1]) == 1;
                        var timestamp = long.Parse(split[2]);
                        ListOLogs.Add(new Log
                        {
                            PlayerCount = playerCount,
                            ServerUp = serverUp,
                            Timestamp = timestamp
                        });
                    }
                }
                catch (FormatException e)
                {
                    _logger.Log(LogSeverity.Warning, $"Failed to convert numbers for this: {data}", e);
                }

                data = await sr.ReadLineAsync();
            }
        }
    }
}