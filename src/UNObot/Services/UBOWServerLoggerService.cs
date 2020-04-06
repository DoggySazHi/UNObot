using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;

namespace UNObot.Services
{
    //TODO Compress the JSON!
    public class UBOWServerLoggerService
    {
        private static readonly string FileName = "UBOWLog.json";
        private static readonly string IP = "108.61.100.48";
        private static readonly ushort QueryPort = 25444 + 1;

        private static UBOWServerLoggerService Instance;
        private readonly Timer LogTimer;
        private ServerLog Logs;
        private readonly Stopwatch DelayChecker;

        private UBOWServerLoggerService()
        {
            LoggerService.Log(LogSeverity.Info, "Loading logger service...");
            LogTimer = new Timer
            {
                AutoReset = true,
                Interval = 1000 * 60
            };
            LogTimer.Elapsed += LogMinute;
            DelayChecker = new Stopwatch();
            Task.Run(ReadLogs);
        }

        private async Task ReadLogs()
        {
            if (!File.Exists(FileName))
            {
                Console.WriteLine("Started new logging service.");
                Logs = new ServerLog
                {
                    ListOLogs = new List<Log>()
                };
                await using var sw = File.Create(FileName);
                await SaveLogs();
                LogTimer.Enabled = true;
                return;
            }
            string Data;
            using (var sr = new StreamReader(FileName))
                Data = await sr.ReadToEndAsync();
            var Result = JsonConvert.DeserializeObject(Data, typeof(ServerLog));
            if (Result is ServerLog LogFile)
                Logs = LogFile;
            else
            {
                LoggerService.Log(LogSeverity.Error, "Failed to read logs! Created new logging service.");
                Logs = new ServerLog
                {
                    ListOLogs = new List<Log>()
                };
            }
            LogTimer.Enabled = true;
            LoggerService.Log(LogSeverity.Info, "UBOWS Logger initialized!");
        }

        private async Task SaveLogs()
        {
            DelayChecker.Restart();
            var Value = JsonConvert.SerializeObject(Logs);
            await using var sw = new StreamWriter(FileName, false);
            await sw.WriteAsync(Value);
            DelayChecker.Stop();
            if(DelayChecker.ElapsedMilliseconds < 1000)
                LoggerService.Log(LogSeverity.Debug, $"Took {DelayChecker.ElapsedMilliseconds}ms to save JSON data.");
            else
                LoggerService.Log(LogSeverity.Warning, $"Took too long to save JSON! ({DelayChecker.ElapsedMilliseconds}ms.)");
        }

        private const int Attempts = 3;

        private async void LogMinute(object sender, ElapsedEventArgs e)
        {
            var Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            byte PlayerCount = 0;
            bool ServerUp = false;

            for (int i = 0; i < Attempts; i++)
            {
                ServerUp = QueryHandlerService.GetInfo(IP, QueryPort, out var Output);
                if (ServerUp)
                {
                    PlayerCount = Output.Players;
                    break;
                }
            }

            Logs.ListOLogs.Add(new Log
            {
                Timestamp = Timestamp,
                PlayerCount = PlayerCount,
                ServerUp = ServerUp
            });
            if (Logs.ListOLogs.Count >= 365 * 24 * 60)
                Logs.ListOLogs.RemoveRange(0, Logs.ListOLogs.Count - 365 * 24 * 60);
            await Task.Run(RecalculateValues);
        }

        private async Task RecalculateValues()
        {
            var TimeNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long TimeHour = 1000 * 60 * 60;
            long TimeDay = TimeHour * 24;
            long TimeWeek = TimeDay * 7;
            long TimeMonth = TimeDay * 30;
            long TimeYear = TimeDay * 365;

            var LastHour = Logs.ListOLogs.FindAll(o => o.Timestamp > TimeNow - TimeHour);
            var LastDay = Logs.ListOLogs.FindAll(o => o.Timestamp > TimeNow - TimeDay);
            var LastWeek = Logs.ListOLogs.FindAll(o => o.Timestamp > TimeNow - TimeWeek);
            var LastMonth = Logs.ListOLogs.FindAll(o => o.Timestamp > TimeNow - TimeMonth);
            var LastYear = Logs.ListOLogs.FindAll(o => o.Timestamp > TimeNow - TimeYear);

            Logs.AverageLastHour = 1.0f * LastHour.Sum(o => o.PlayerCount) / LastHour.Count;
            Logs.AverageLast24H = 1.0f * LastDay.Sum(o => o.PlayerCount) / LastDay.Count;
            Logs.AverageLastWeek = 1.0f * LastWeek.Sum(o => o.PlayerCount) / LastWeek.Count;
            Logs.AverageLastMonth = 1.0f * LastMonth.Sum(o => o.PlayerCount) / LastMonth.Count;
            Logs.AverageLastYear = 1.0f * LastYear.Sum(o => o.PlayerCount) / LastYear.Count;

            await SaveLogs();
        }

        public static UBOWServerLoggerService GetSingleton()
        {
            if (Instance == null)
                Instance = new UBOWServerLoggerService();
            return Instance;
        }

        public ServerAverages GetAverages()
        {
            return new ServerAverages
            {
                AverageLastHour = Logs.AverageLastHour,
                AverageLast24H = Logs.AverageLast24H,
                AverageLastWeek = Logs.AverageLastWeek,
                AverageLastMonth = Logs.AverageLastMonth,
                AverageLastYear = Logs.AverageLastYear
            };
        }
    }

    public struct Log
    {
        public long Timestamp { get; set; }
        public byte PlayerCount { get; set; }
        public bool ServerUp { get; set; }
    }

    public class ServerAverages
    {
        public float AverageLastHour { get; set; }
        public float AverageLast24H { get; set; }
        public float AverageLastWeek { get; set; }
        public float AverageLastMonth { get; set; }
        public float AverageLastYear { get; set; }
    }

    public class ServerLog
    {
        public float AverageLastHour { get; set; }
        public float AverageLast24H { get; set; }
        public float AverageLastWeek { get; set; }
        public float AverageLastMonth { get; set; }
        public float AverageLastYear { get; set; }
        public List<Log> ListOLogs { get; set; }
    }
}
