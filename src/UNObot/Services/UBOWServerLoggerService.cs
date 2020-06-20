using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Newtonsoft.Json;

namespace UNObot.Services
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

        private UBOWServerLoggerService()
        {
            LoggerService.Log(LogSeverity.Info, "Loading logger service...");
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
                LoggerService.Log(LogSeverity.Info, "Found old JSON file, please wait as we upgrade...");
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
                    LoggerService.Log(LogSeverity.Info, "Successfully upgraded file!");
                }
                else
                {
                    LoggerService.Log(LogSeverity.Error, "Failed to read logs! Created new logging service.");
                    File.Delete(JsonFileName);
                }
            }

            if (!File.Exists(FileName) && !File.Exists(JsonFileName))
            {
                LoggerService.Log(LogSeverity.Info, "Started new logging service.");
                _logs = new ServerLog
                {
                    ListOLogs = new List<Log>()
                };
                await _logs.WriteToFile(FileName);
                _logTimer.Enabled = true;
                return;
            }

            _logs = new ServerLog();
            await _logs.ReadFromFile(FileName);
            _logTimer.Enabled = true;
            LoggerService.Log(LogSeverity.Info, "UBOWS Logger initialized!");
        }

        private void LogMinute(object sender, ElapsedEventArgs e)
        {
#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                byte playerCount = 0;
                var serverUp = false;

                for (var i = 0; i < Attempts; i++)
                {
                    serverUp = QueryHandlerService.GetInfo(Ip, QueryPort, out var output);
                    if (serverUp)
                    {
                        playerCount = output.Players;
                        break;
                    }
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

        public ServerAverages GetAverages()
        {
            return new ServerAverages
            {
                AverageLastHour = _logs.AverageLastHour,
                AverageLast24H = _logs.AverageLast24H,
                AverageLastWeek = _logs.AverageLastWeek,
                AverageLastMonth = _logs.AverageLastMonth,
                AverageLastYear = _logs.AverageLastYear
            };
        }
    }

    public struct Log
    {
        public long Timestamp { get; set; }
        public byte PlayerCount { get; set; }
        public bool ServerUp { get; set; }
    }

    internal class ServerAverages
    {
        public float AverageLastHour { get; set; }
        public float AverageLast24H { get; set; }
        public float AverageLastWeek { get; set; }
        public float AverageLastMonth { get; set; }
        public float AverageLastYear { get; set; }
    }

    internal class ServerLog
    {
        public float AverageLastHour { get; set; }
        public float AverageLast24H { get; set; }
        public float AverageLastWeek { get; set; }
        public float AverageLastMonth { get; set; }
        public float AverageLastYear { get; set; }
        public List<Log> ListOLogs { get; set; }

        public async Task WriteToFile(string fileName)
        {
            await using var sw = new StreamWriter(fileName);
            await sw.WriteLineAsync("LH" + AverageLastHour);
            await sw.WriteLineAsync("LD" + AverageLast24H);
            await sw.WriteLineAsync("LW" + AverageLastWeek);
            await sw.WriteLineAsync("LM" + AverageLastMonth);
            await sw.WriteLineAsync("LY" + AverageLastYear);
            foreach (var item in ListOLogs) await AppendToFile(fileName, item, sw);
        }

        public async Task AppendToFile(string fileName, Log log, StreamWriter sw = null)
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

        public async Task ReadFromFile(string fileName)
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
                            LoggerService.Log(LogSeverity.Warning, $"Invalid record in file! Read {data}.");
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
                    LoggerService.Log(LogSeverity.Warning, $"Failed to convert numbers for this: {data}", e);
                }

                data = await sr.ReadLineAsync();
            }
        }
    }
}