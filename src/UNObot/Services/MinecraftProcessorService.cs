using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static UNObot.Services.IRCON;

namespace UNObot.Services
{
    public class MCUser
    {
        public string Username { get; set; }
        public string Ouchies { get; set; }
        public bool Online { get; set; }
        public double[] Coordinates { get; set; }
        public string Health { get; set; }
        public string Food { get; set; }
        public int Experience { get; set; }
    }
    
    public static class MinecraftProcessorService
    {
        // NOTE: It's the query port!
        public static List<MCUser> GetMCUsers(string IP, ushort Port, string Password, out IRCON Client, bool Dispose = true)
        {
            var Output = new List<MCUser>();

            // Smaller than ulong keys, big enough for RNG.
            var random = new Random();
            var RandomKey = (ulong) random.Next(0, 10000);
            var Success = QueryHandlerService.CreateRCON(IP, Port, Password, RandomKey, out Client);
            if (!Success) return Output;

            Client.ExecuteSingle("list", true);
            if (Client.Status != RCONStatus.SUCCESS) return Output;
            var PlayerListOnline = Client.Data.Substring(Client.Data.IndexOf(':') + 1).Split(',').ToList();

            Client.ExecuteSingle("scoreboard players list", true);
            var PlayerListTotal = Client.Data.Substring(Client.Data.IndexOf(':') + 1).Split(',').ToList();

            foreach(var Player in PlayerListTotal)
            {
                var Name = Player.Replace((char) 0, ' ').Trim();
                Client.ExecuteSingle($"scoreboard players get {Name} Ouchies", true);
                if (Client.Status == RCONStatus.SUCCESS)
                {
                    var Ouchies = Client.Data.Contains("has") ? Client.Data.Split(' ')[2] : "0";
                    Output.Add(new MCUser
                    {
                        Username = Name,
                        Ouchies = Ouchies
                    });
                }
            }

            var RandomKey2 = (ulong) random.Next(0, 10000);
            var Success2 = QueryHandlerService.CreateRCON(IP, Port, Password, RandomKey, out var Client2);
            if (!Success2) return Output;
            
            foreach (var o in PlayerListOnline)
            {
                var Name = o.Replace((char) 0, ' ').Trim();
                if (string.IsNullOrWhiteSpace(Name)) continue;
                
                /*
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    LoggerService.Log(LogSeverity.Warning, "Linux platform: safety to use old parser.");
                    OldUserProcessor(ref Output, Name, Client);
                    continue;
                }
                */

                var Command = $"data get entity {Name}";
                Client2.Execute(Command, true);
                
                if (Client.Status != RCONStatus.SUCCESS)
                    continue;
                if (Client.Data.Equals("No entity was found", StringComparison.CurrentCultureIgnoreCase))
                    continue;

                // Ignore the (PLAYERNAME) has the following data:
                LoggerService.Log(LogSeverity.Debug, Command + " | " + Client.Data);
                var JSONString = Client.Data.Substring(Client.Data.IndexOf('{'));
                // For UUIDs, they start an array with I; to indicate all values are integers; ignore it.
                JSONString = JSONString.Replace("I;", "");
                // Regex to completely ignore the b, s, l, f, and d patterns. Probably the worst RegEx I ever wrote.
                JSONString = Regex.Replace(JSONString, @"([:|\[|\,]\s*\-?\d*.?\d+)[s|b|l|f|d|L]", "${1}");
                
                try
                {
                    double[] Coordinates = null;

                    /*
                    JObject JSON;
                    try
                    {
                        JSON = JObject.Parse(JSONString);
                    }
                    catch (JsonReaderException ex)
                    {
                        LoggerService.Log(LogSeverity.Error, $@"Error near string: {JSONString.Substring(
                            Math.Max(0, ex.LinePosition - 10), Math.Min(20, JSONString.Length - ex.LinePosition - 10)
                        )}", ex);
                        
                        throw;
                    }
                    */
                    
                    JToken JSON;
                    string exceptionPath = null;
                    using (var textReader = new StringReader(JSONString))
                    using (var jsonReader = new JsonTextReader(textReader))
                    using (var jsonWriter = new JTokenWriter())
                    {
                        try
                        {
                            jsonWriter.WriteToken(jsonReader);
                        }
                        catch (JsonReaderException ex)
                        {
                            exceptionPath = ex.Path;
                            LoggerService.Log(LogSeverity.Error, $@"Error near string: {JSONString.Substring(
                                Math.Max(0, ex.LinePosition - 10), Math.Min(20, JSONString.Length - ex.LinePosition - 10)
                            )}", ex);
                        }
                        JSON = jsonWriter.Token;
                    }

                    if (exceptionPath != null)
                    {
                        var badToken = JSON.SelectToken(exceptionPath);
                        LoggerService.Log(LogSeverity.Error, $"Error occurred with token: {badToken}");
                    }
                    
                    var Dimension = JSON["Dimension"];
                    var Position = JSON["Pos"];
                    var Food = JSON["foodLevel"]?.ToObject<string>() ?? "20";
                    var HealthNum = JSON["Health"]?.ToObject<float>();
                    var XPLevels = JSON["XpLevel"].ToObject<int>();
                    var XPPercent = JSON["XpP"].ToObject<float>();
                    var XPPoints = (Exp(XPLevels + 1, 0) - Exp(XPLevels, 0)) * XPPercent;
                    var Experience = (int) Exp(XPLevels, (int) Math.Floor(XPPoints));
                    var Health = HealthNum != null ? Math.Ceiling((float) HealthNum).ToString("#") : "20";

                    if (Position?[0] != null && Position[1] != null && Position[2] != null && Dimension != null)
                    {
                        var DimensionWord = Dimension.ToString();
                        if (DimensionWord.Contains("overworld"))
                            Dimension = 0;
                        else if (DimensionWord.Contains("nether"))
                            Dimension = -1;
                        else if (DimensionWord.Contains("end"))
                            Dimension = 1;
                        Coordinates = new[]
                        {
                            Position[0].ToObject<double>(),
                            Position[1].ToObject<double>(),
                            Position[2].ToObject<double>(),
                            Dimension.ToObject<double>()
                        };
                    }

                    var CorrectUser = Output.Find(User => User.Username == Name);
                    if (CorrectUser != null)
                    {
                        CorrectUser.Coordinates = Coordinates;
                        CorrectUser.Health = Health;
                        CorrectUser.Food = Food;
                        CorrectUser.Online = true;
                        CorrectUser.Experience = Experience;
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.Log(LogSeverity.Error, $"Failed to process JSON! Falling back...\n{JSONString}", ex);
                    OldUserProcessor(ref Output, Name, Client);
                }
            }

            if(Dispose)
                Client.Dispose();
            return Output;
        }

        private static void OldUserProcessor(ref List<MCUser> Users, string Name, IRCON Client)
        {
            Client.ExecuteSingle(
                    $"execute as {Name} at @s run summon minecraft:armor_stand ~ ~ ~ {{Invisible:1b,PersistenceRequired:1b,Tags:[\"coordfinder\"]}}",
                    true);
            Client.ExecuteSingle("execute as @e[tag=coordfinder] at @s run tp @s ~ ~ ~", true);
            double[] Coordinates = null;
            if (Client.Status == RCONStatus.SUCCESS)
            {
                try
                {
                    var CoordinateMessage = Regex.Replace(Client.Data, @"[^0-9\+\-\. ]", "").Trim().Split(' ');
                    if (CoordinateMessage.Length == 3)
                        Coordinates = new[]
                        {
                            double.Parse(CoordinateMessage[0]),
                            double.Parse(CoordinateMessage[1]),
                            double.Parse(CoordinateMessage[2])
                        };
                    else
                        Coordinates = new[] {0.0, 0.0, 0.0};
                }
                catch (FormatException)
                {
                    LoggerService.Log(LogSeverity.Warning, $"Failed to process coordinates. Response: {Client.Data}");
                }
            }
            Client.ExecuteSingle("execute as @e[tag=coordfinder] at @s run kill @s", true);

            Client.ExecuteSingle($"scoreboard players get {Name} Health", true);
            var Health = Client.Data.Contains("has") ? Client.Data.Split(' ')[2] : "??";
            Client.ExecuteSingle($"scoreboard players get {Name} Food", true);
            var Food = Client.Data.Contains("has") ? Client.Data.Split(' ')[2] : "??";
            
            Client.ExecuteSingle($"execute as {Name} at @s run experience query @s points", true);
            var PointData = Client.Data;
            Client.ExecuteSingle($"execute as {Name} at @s run experience query @s levels", true);
            var Experience = 0;
            if (Client.Status == RCONStatus.SUCCESS)
            {
                try
                {
                    var Points = int.Parse(PointData.Split(' ')[2]);
                    var Levels = int.Parse(Client.Data.Split(' ')[2]);
                    Experience = (int) Exp(Levels, Points);
                }
                catch (FormatException)
                {
                    LoggerService.Log(LogSeverity.Warning, $"Failed to process experience. Response: {Client.Data}");
                }
            }
            foreach (var CorrectUser in Users.Where(User => User.Username == Name))
            {
                CorrectUser.Coordinates = Coordinates;
                CorrectUser.Health = Health;
                CorrectUser.Food = Food;
                CorrectUser.Online = true;
                CorrectUser.Experience = Experience;
            }
        }
        
        private static double Exp(int levels, int points)
        {
            if(levels <= 16)
                return Math.Pow(levels, 2) + 6 * levels + points;
            if (levels <= 31)
                return 2.5 * Math.Pow(levels, 2) - 40.5 * levels + 360 + points;
            return 4.5 * Math.Pow(levels, 2) - 162.5 * levels + 2220 + points;
        }
    }
}