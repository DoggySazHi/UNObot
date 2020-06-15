using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Discord;

namespace UNObot.Services
{
    public class UBOWServerLoggerService
    {
        private const string JSONFileName = "UBOWLog.json";
        private const string FileName = "UBOWLog.serverlog";
        private const string IP = "108.61.100.48";
        private const ushort QueryPort = 25444 + 1;

        private static UBOWServerLoggerService Instance;
        private readonly Timer LogTimer;
        private ServerLog Logs;

        private UBOWServerLoggerService()
        {
            LoggerService.Log(LogSeverity.Info, "Loading logger service...");
            LogTimer = new Timer
            {
                AutoReset = true,
                Interval = 1000 * 60
            };
            LogTimer.Elapsed += LogMinute;
            Task.Run(ReadLogs);
        }

        private async Task ReadLogs()
        {
            if (!File.Exists(FileName) && File.Exists(JSONFileName))
            {
                LoggerService.Log(LogSeverity.Info, "Found old JSON file, please wait as we upgrade...");
                string Data;
                using (var sr = new StreamReader(JSONFileName))
                    Data = await sr.ReadToEndAsync();
                var Result = JsonConvert.DeserializeObject(Data, typeof(ServerLog));
                if (Result is ServerLog LogFile)
                {
                    Logs = LogFile;
                    await Logs.WriteToFile(FileName);
                    LoggerService.Log(LogSeverity.Info, "Successfully upgraded file!");
                }
                else
                {
                    LoggerService.Log(LogSeverity.Error, "Failed to read logs! Created new logging service.");
                    File.Delete(JSONFileName);
                }
            }
            if (!File.Exists(FileName) && !File.Exists(JSONFileName))
            {
                LoggerService.Log(LogSeverity.Info, "Started new logging service.");
                Logs = new ServerLog
                {
                    ListOLogs = new List<Log>()
                };
                await Logs.WriteToFile(FileName);
                LogTimer.Enabled = true;
                return;
            }
            Logs = new ServerLog();
            await Logs.ReadFromFile(FileName);
            LogTimer.Enabled = true;
            LoggerService.Log(LogSeverity.Info, "UBOWS Logger initialized!");
        }

        private const int Attempts = 3;

        private void LogMinute(object sender, ElapsedEventArgs e)
        {
#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                 var Timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                 byte PlayerCount = 0;
                 var ServerUp = false;

                 for (var i = 0; i < Attempts; i++)
                 {
                     ServerUp = QueryHandlerService.GetInfo(IP, QueryPort, out var Output);
                     if (ServerUp)
                     {
                         PlayerCount = Output.Players;
                         break;
                     }
                 }

                 var NowLog = new Log
                 {
                     Timestamp = Timestamp,
                     PlayerCount = PlayerCount,
                     ServerUp = ServerUp
                 };
                 Logs.ListOLogs.Add(NowLog);
                 await Logs.AppendToFile(FileName, NowLog);
                 if (Logs.ListOLogs.Count >= 365 * 24 * 60)
                     Logs.ListOLogs.RemoveRange(0, Logs.ListOLogs.Count - 365 * 24 * 60);
                 RecalculateValues();
             });
        }

        private void RecalculateValues()
        {
            var TimeNow = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            const long TimeHour = 1000 * 60 * 60;
            const long TimeDay = TimeHour * 24;
            const long TimeWeek = TimeDay * 7;
            const long TimeMonth = TimeDay * 30;
            const long TimeYear = TimeDay * 365;

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
        }

        public static UBOWServerLoggerService GetSingleton()
        {
            return Instance ??= new UBOWServerLoggerService();
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

        public async Task WriteToFile(string FileName)
        {
            await using var sw = new StreamWriter(FileName);
            await sw.WriteLineAsync("LH" + AverageLastHour);
            await sw.WriteLineAsync("LD" + AverageLast24H);
            await sw.WriteLineAsync("LW" + AverageLastWeek);
            await sw.WriteLineAsync("LM" + AverageLastMonth);
            await sw.WriteLineAsync("LY" + AverageLastYear);
            foreach (var item in ListOLogs)
            {
                await AppendToFile(FileName, item, sw);
            }
        }

        public async Task AppendToFile(string FileName, Log Log, StreamWriter sw = null)
        {
            var selfCreated = false;
            if (sw == null)
            {
                sw = new StreamWriter(FileName, true);
                selfCreated = true;
            }

            var sb = new StringBuilder("L", 16);
            sb.Append(Log.PlayerCount);
            sb.Append(",");
            sb.Append(Log.ServerUp ? 1 : 0);
            sb.Append(",");
            sb.Append(Log.Timestamp);
            await sw.WriteLineAsync(sb);

            if (selfCreated)
                await sw.DisposeAsync();
        }

        public async Task ReadFromFile(string FileName)
        {
            if(ListOLogs == null)
                ListOLogs = new List<Log>();
            else if (ListOLogs.Count != 0)
                ListOLogs.Clear();
            using var sr = new StreamReader(FileName);
            var Data = await sr.ReadLineAsync();
            while (Data != null)
            {
                try
                {
                    if (Data.StartsWith("LH"))
                        AverageLastHour = float.Parse(Data.Substring(2));
                    else if (Data.StartsWith("LD"))
                        AverageLast24H = float.Parse(Data.Substring(2));
                    else if (Data.StartsWith("LW"))
                        AverageLastWeek = float.Parse(Data.Substring(2));
                    else if (Data.StartsWith("LM"))
                        AverageLastMonth = float.Parse(Data.Substring(2));
                    else if (Data.StartsWith("LY"))
                        AverageLastYear = float.Parse(Data.Substring(2));
                    else if (Data.StartsWith("L"))
                    {
                        var Split = Data.Substring(1).Split(",");
                        if (Split.Length != 3)
                        {
                            LoggerService.Log(LogSeverity.Warning, $"Invalid record in file! Read {Data}.");
                            continue;
                        }

                        var PlayerCount = byte.Parse(Split[0]);
                        var ServerUp = int.Parse(Split[1]) == 1;
                        var Timestamp = long.Parse(Split[2]);
                        ListOLogs.Add(new Log
                        {
                            PlayerCount = PlayerCount,
                            ServerUp = ServerUp,
                            Timestamp = Timestamp
                        });
                    }
                }
                catch (FormatException e)
                {
                    LoggerService.Log(LogSeverity.Warning, $"Failed to convert numbers for this: {Data}", e);
                }

                Data = await sr.ReadLineAsync();
            }
        }
    }
}
