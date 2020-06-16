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
        public static List<MCUser> GetMCUsers(string ip, ushort port, string password, out IRCON client,
            bool dispose = true)
        {
            var output = new List<MCUser>();

            // Smaller than ulong keys, big enough for RNG.
            var random = new Random();
            var randomKey = (ulong) random.Next(0, 10000);
            var success = QueryHandlerService.CreateRCON(ip, port, password, randomKey, out client);
            if (!success)
            {
                LoggerService.Log(LogSeverity.Error, "Failed to create an RCON connection to get players!");
                return output;
            }

            client.ExecuteSingle("list", true);
            if (client.Status != RCONStatus.Success) return output;
            var playerListOnline = client.Data.Substring(client.Data.IndexOf(':') + 1).Split(',').ToList();

            client.ExecuteSingle("scoreboard players list", true);
            var playerListTotal = client.Data.Substring(client.Data.IndexOf(':') + 1).Split(',').ToList();

            foreach (var player in playerListTotal)
            {
                var name = player.Replace((char) 0, ' ').Trim();
                client.ExecuteSingle($"scoreboard players get {name} Ouchies", true);
                if (client.Status == RCONStatus.Success)
                {
                    var ouchies = client.Data.Contains("has") ? client.Data.Split(' ')[2] : "0";
                    output.Add(new MCUser
                    {
                        Username = name,
                        Ouchies = ouchies
                    });
                }
            }

            foreach (var o in playerListOnline)
            {
                var name = o.Replace((char) 0, ' ').Trim();
                if (string.IsNullOrWhiteSpace(name)) continue;

                /*
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    LoggerService.Log(LogSeverity.Warning, "Linux platform: safety to use old parser.");
                    OldUserProcessor(ref Output, Name, Client);
                    continue;
                }
                */

                var command = $"data get entity {name}";
                var randomKey2 = (ulong) random.Next(0, 10000);
                var success2 = QueryHandlerService.CreateRCON(ip, port, password, randomKey2, out var client2);
                if (!success2)
                {
                    LoggerService.Log(LogSeverity.Error,
                        "Failed to create a second RCON connection to get player data!");
                    return output;
                }

                client2.Execute(command, true);

                if (client2.Status != RCONStatus.Success)
                    continue;
                if (client2.Data.Equals("No entity was found", StringComparison.CurrentCultureIgnoreCase))
                    continue;

                var jsonString = "No string was found.";
                try
                {
                    // Ignore the (PLAYERNAME) has the following data:
                    LoggerService.Log(LogSeverity.Debug, command + " | " + client2.Data);
                    jsonString = client2.Data.Substring(client2.Data.IndexOf('{'));
                    // For UUIDs, they start an array with I; to indicate all values are integers; ignore it.
                    jsonString = jsonString.Replace("I;", "");
                    // Regex to completely ignore the b, s, l, f, and d patterns. Probably the worst RegEx I ever wrote.
                    jsonString = Regex.Replace(jsonString, @"([:|\[|\,]\s*\-?\d*.?\d+)[s|b|l|f|d|L]", "${1}");

                    double[] coordinates = null;

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

                    JToken json;
                    string exceptionPath = null;
                    using (var textReader = new StringReader(jsonString))
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
                            LoggerService.Log(LogSeverity.Error, $@"Error near string: {jsonString.Substring(
                                Math.Max(0, ex.LinePosition - 10), Math.Min(20, jsonString.Length - ex.LinePosition - 10)
                            )}", ex);
                        }

                        json = jsonWriter.Token;
                    }

                    if (exceptionPath != null)
                    {
                        var badToken = json.SelectToken(exceptionPath);
                        LoggerService.Log(LogSeverity.Error, $"Error occurred with token: {badToken}");
                    }

                    var dimension = json["Dimension"];
                    var position = json["Pos"];
                    var food = json["foodLevel"]?.ToObject<string>() ?? "20";
                    var healthNum = json["Health"]?.ToObject<float>();
                    var xpLevels = json["XpLevel"].ToObject<int>();
                    var xpPercent = json["XpP"].ToObject<float>();
                    var xpPoints = (Exp(xpLevels + 1, 0) - Exp(xpLevels, 0)) * xpPercent;
                    var experience = (int) Exp(xpLevels, (int) Math.Floor(xpPoints));
                    var health = healthNum != null ? Math.Ceiling((float) healthNum).ToString("#") : "20";

                    if (position?[0] != null && position[1] != null && position[2] != null && dimension != null)
                    {
                        var dimensionWord = dimension.ToString();
                        if (dimensionWord.Contains("overworld"))
                            dimension = 0;
                        else if (dimensionWord.Contains("nether"))
                            dimension = -1;
                        else if (dimensionWord.Contains("end"))
                            dimension = 1;
                        coordinates = new[]
                        {
                            position[0].ToObject<double>(),
                            position[1].ToObject<double>(),
                            position[2].ToObject<double>(),
                            dimension.ToObject<double>()
                        };
                    }

                    var correctUser = output.Find(user => user.Username == name);
                    if (correctUser != null)
                    {
                        correctUser.Coordinates = coordinates;
                        correctUser.Health = health;
                        correctUser.Food = food;
                        correctUser.Online = true;
                        correctUser.Experience = experience;
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.Log(LogSeverity.Error, $"Failed to process JSON! Falling back...\n{jsonString}", ex);
                    OldUserProcessor(ref output, name, client);
                }

                client2.Dispose();
            }

            if (dispose)
                client.Dispose();
            return output;
        }

        private static void OldUserProcessor(ref List<MCUser> users, string name, IRCON client)
        {
            client.ExecuteSingle(
                $"execute as {name} at @s run summon minecraft:armor_stand ~ ~ ~ {{Invisible:1b,PersistenceRequired:1b,Tags:[\"coordfinder\"]}}",
                true);
            client.ExecuteSingle("execute as @e[tag=coordfinder] at @s run tp @s ~ ~ ~", true);
            var coordinates = new double[4];
            if (client.Status == RCONStatus.Success)
                try
                {
                    var coordinateMessage = Regex.Replace(client.Data, @"[^0-9\+\-\. ]", "").Trim().Split(' ');
                    if (coordinateMessage.Length == 3)
                        coordinates = new[]
                        {
                            double.Parse(coordinateMessage[0]),
                            double.Parse(coordinateMessage[1]),
                            double.Parse(coordinateMessage[2]),
                            -1
                        };
                    else
                        coordinates = new[] {0.0, 0.0, 0.0, -1};
                }
                catch (FormatException)
                {
                    LoggerService.Log(LogSeverity.Warning, $"Failed to process coordinates. Response: {client.Data}");
                }

            client.ExecuteSingle(
                $"execute as @e[tag=coordfinder] at @s in the_nether run execute as @a[name={name}, distance=..1] run tag @e[tag=coordfinder] add found",
                true);
            LoggerService.Log(LogSeverity.Debug, $"Armor-stand for dimension check: {client.Data}");
            client.ExecuteSingle("tag @e[tag=cooordfinder] list", true);
            LoggerService.Log(LogSeverity.Debug, $"Armor-stand for dimension check: {client.Data}");
            if (!client.Data.Contains("found"))
            {
                coordinates[3] = 1;
                client.ExecuteSingle(
                    $"execute as @e[tag=coordfinder] at @s in the_end run execute as @a[name={name}, distance=..1] run tag @e[tag=coordfinder] add found",
                    true);
                LoggerService.Log(LogSeverity.Debug, $"Armor-stand for dimension check: {client.Data}");
                client.ExecuteSingle("tag @e[tag=cooordfinder] list", true);
                LoggerService.Log(LogSeverity.Debug, $"Armor-stand for dimension check: {client.Data}");
                if (!client.Data.Contains("found"))
                    coordinates[3] = 0;
            }

            client.ExecuteSingle("execute as @e[tag=coordfinder] at @s run kill @s", true);

            client.ExecuteSingle($"scoreboard players get {name} Health", true);
            var health = client.Data.Contains("has") ? client.Data.Split(' ')[2] : "??";
            client.ExecuteSingle($"scoreboard players get {name} Food", true);
            var food = client.Data.Contains("has") ? client.Data.Split(' ')[2] : "??";

            client.ExecuteSingle($"execute as {name} at @s run experience query @s points", true);
            var pointData = client.Data;
            client.ExecuteSingle($"execute as {name} at @s run experience query @s levels", true);
            var experience = 0;
            if (client.Status == RCONStatus.Success)
                try
                {
                    var points = int.Parse(pointData.Split(' ')[2]);
                    var levels = int.Parse(client.Data.Split(' ')[2]);
                    experience = (int) Exp(levels, points);
                }
                catch (FormatException)
                {
                    LoggerService.Log(LogSeverity.Warning, $"Failed to process experience. Response: {client.Data}");
                }

            foreach (var correctUser in users.Where(user => user.Username == name))
            {
                correctUser.Coordinates = coordinates;
                correctUser.Health = health;
                correctUser.Food = food;
                correctUser.Online = true;
                correctUser.Experience = experience;
            }
        }

        private static double Exp(int levels, int points)
        {
            if (levels <= 16)
                return Math.Pow(levels, 2) + 6 * levels + points;
            if (levels <= 31)
                return 2.5 * Math.Pow(levels, 2) - 40.5 * levels + 360 + points;
            return 4.5 * Math.Pow(levels, 2) - 162.5 * levels + 2220 + points;
        }
    }
}