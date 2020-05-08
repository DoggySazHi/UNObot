using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;

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
        public static List<MCUser> GetMCUsers(string IP, ushort Port, string Password, out MinecraftRCON Client, bool Dispose = true)
        {
            var Output = new List<MCUser>();

            // Smaller than ulong keys, big enough for RNG.
            var RandomKey = (ulong) new Random().Next(0, 10000);
            var Success = QueryHandlerService.CreateRCON(IP, Port, Password, RandomKey, out Client);
            if (!Success) return Output;

            Client.Execute("list", true);
            if (Client.Status != MinecraftRCON.RCONStatus.SUCCESS) return Output;
            var PlayerListOnline = Client.Data.Substring(Client.Data.IndexOf(':') + 1).Split(',').ToList();

            Client.Execute("scoreboard players list", true);
            var PlayerListTotal = Client.Data.Substring(Client.Data.IndexOf(':') + 1).Split(',').ToList();

            foreach(var Player in PlayerListTotal)
            {
                var Name = Player.Replace((char) 0, ' ').Trim();
                Client.Execute($"scoreboard players get {Name} Ouchies", true);
                if (Client.Status == MinecraftRCON.RCONStatus.SUCCESS)
                {
                    var Ouchies = Client.Data.Contains("has") ? Client.Data.Split(' ')[2] : "0";
                    Output.Add(new MCUser
                    {
                        Username = Name,
                        Ouchies = Ouchies
                    });
                }
            }

            foreach (var o in PlayerListOnline)
            {
                var Name = o.Replace((char) 0, ' ').Trim();
                if (string.IsNullOrWhiteSpace(Name)) continue;
                Client.Execute(
                    $"execute as {Name} at @s run summon minecraft:armor_stand ~ ~ ~ {{Invisible:1b,PersistenceRequired:1b,Tags:[\"coordfinder\"]}}",
                    true);
                Client.Execute("execute as @e[tag=coordfinder] at @s run tp @s ~ ~ ~", true);
                double[] Coordinates = null;
                if (Client.Status == MinecraftRCON.RCONStatus.SUCCESS)
                {
                    try
                    {
                        var CoordinateMessage = Regex.Replace(Client.Data, @"[^0-9\+\-\. ]", "").Trim().Split(' ');
                        Coordinates = new[]
                        {
                            double.Parse(CoordinateMessage[0]),
                            double.Parse(CoordinateMessage[1]),
                            double.Parse(CoordinateMessage[2])
                        };
                    }
                    catch (FormatException)
                    {
                        LoggerService.Log(LogSeverity.Warning, $"Failed to process coordinates. Response: {Client.Data}");
                    }
                }
                Client.Execute($"scoreboard players get {Name} Health", true);
                var Health = Client.Data.Contains("has") ? Client.Data.Split(' ')[2] : "??";
                Client.Execute($"scoreboard players get {Name} Food", true);
                var Food = Client.Data.Contains("has") ? Client.Data.Split(' ')[2] : "??";
                Client.Execute("execute as @e[tag=coordfinder] at @s run kill @s", true);
                Client.Execute($"execute as {Name} at @s run experience query @s points", true);
                var PointData = Client.Data;
                Client.Execute($"execute as {Name} at @s run experience query @s levels", true);
                var Experience = 0;
                if (Client.Status == MinecraftRCON.RCONStatus.SUCCESS)
                {
                    try
                    {
                        var Points = int.Parse(PointData.Split(' ')[2]);
                        var Levels = int.Parse(Client.Data.Split(' ')[2]);
                        Experience = (int) Exp(Levels, Points);
                    }
                    catch (FormatException)
                    {
                        LoggerService.Log(LogSeverity.Warning, $"Failed to process coordinates. Response: {Client.Data}");
                    }
                }
                foreach (var CorrectUser in Output.Where(User => User.Username == Name))
                {
                    CorrectUser.Coordinates = Coordinates;
                    CorrectUser.Health = Health;
                    CorrectUser.Food = Food;
                    CorrectUser.Online = true;
                    CorrectUser.Experience = Experience;
                }
            }

            if(Dispose)
                Client.Dispose();
            return Output;
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