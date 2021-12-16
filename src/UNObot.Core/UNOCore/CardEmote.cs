using System.Collections.Generic;
using Discord;

namespace UNObot.Core.UNOCore;

public static class CardEmote
{
    private static readonly IReadOnlyDictionary<string, ulong> EmoteIDs = new Dictionary<string, ulong>
    {
        // Blue Cards
        { "Blue_0", 876699976678408212 }, { "Blue_1", 876699978104442900 }, { "Blue_+2", 876699976720322621 },
        { "Blue_2", 876699980365189150 }, { "Blue_3", 876699980696530965 }, { "Blue_4", 876699979782164480 },
        { "Blue_5", 876699981816397854 }, { "Blue_6", 876699985880690708 }, { "Blue_7", 876699982374252554 },
        { "Blue_8", 876699986174291978 }, { "Blue_9", 876699986493046834 }, { "Blue_Any", 876856627045748777 },
        { "Blue_Reverse", 876699985561923634 }, { "Blue_Skip", 876856627939131412 },
            
        // Green Cards
        { "Green_0", 876699988040749096 }, { "Green_1", 876699985285095445 }, { "Green_+2", 876699988103688252 },
        { "Green_2", 876699988363735110 }, { "Green_3", 876699987977842699 }, { "Green_4", 876699987260637221 },
        { "Green_5", 876699987562614786 }, { "Green_6", 876699988548280330 }, { "Green_7", 876699986732130335 },
        { "Green_8", 876699988460187648 }, { "Green_9", 876699988737003520 }, { "Green_Any", 876856627762987008 },
        { "Green_Reverse", 876699986958614548 }, { "Green_Skip", 876856627960107018 },
            
        // Red Cards
        { "Red_0", 876699987436793856 }, { "Red_1", 876699984903434251 }, { "Red_+2", 876699987738775552 },
        { "Red_2", 876699987269013554 }, { "Red_3", 876699987860410479 }, { "Red_4", 876699986618884126 },
        { "Red_5", 876699987092856882 }, { "Red_6", 876699988250472498 }, { "Red_7", 876699985868107797 },
        { "Red_8", 876853785224413204 }, { "Red_9", 876699988133040188 }, { "Red_Any", 876856627842662460 },
        { "Red_Reverse", 876699986715357184 }, { "Red_Skip", 876856628119490610 },
            
        // Yellow Cards
        { "Yellow_0", 876699987537440818 }, { "Yellow_1", 876699985830355004 }, { "Yellow_+2", 876699988288225280 },
        { "Yellow_2", 876699987587780628 }, { "Yellow_3", 876699988049149972 }, { "Yellow_4", 876699987080282142 },
        { "Yellow_5", 876699987315146772 }, { "Yellow_6", 876699988082720830 }, { "Yellow_7", 876699985830346762 },
        { "Yellow_8", 876699987889762355 }, { "Yellow_9", 876699988271443988 }, { "Yellow_Any", 876856628308217937 },
        { "Yellow_Reverse", 876699986757308436 }, { "Yellow_Skip", 876856628689899620 },
            
        // Wild Cards
        { "Wild_4", 876699987923304519 }, { "Wild_Color", 876853986953666632 }
    };

    private static readonly Dictionary<string, IEmote> Emotes = new();

    static CardEmote()
    {
        foreach (var (name, id) in EmoteIDs)
            Emotes[name] = Emote.Parse($"<${name.Contains("Skip") || name.Contains("Any")}:{name.Replace("+", "")}:{id}>");
    }
        
    public static IEmote GetEmote(string name)
    {
        return !Emotes.ContainsKey(name) ? null : Emotes[name];
    }
}