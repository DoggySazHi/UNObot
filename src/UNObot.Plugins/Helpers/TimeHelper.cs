using System;

namespace UNObot.Plugins.Helpers;

public class TimeHelper
{
    public static string HumanReadable(float time)
    {
        var formatted = TimeSpan.FromSeconds(time);
        string output;
        if (formatted.Hours != 0)
            output = $"{(int) formatted.TotalHours}:{formatted.Minutes:00}:{formatted.Seconds:00}";
        else if (formatted.Minutes != 0)
            output = $"{formatted.Minutes}:{formatted.Seconds:00}";
        else
            output = $"{formatted.Seconds} second{(formatted.Seconds == 1 ? "" : "s")}";
        return output;
    }
}