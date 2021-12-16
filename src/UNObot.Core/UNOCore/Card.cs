using System;
using Newtonsoft.Json;
using UNObot.Plugins.TerminalCore;

namespace UNObot.Core.UNOCore;

public class Card
{
    public readonly string Color;
    public readonly string Value;

    [JsonConstructor]
    public Card(string color, string value)
    {
        Color = color;
        Value = value;
    }

    public override string ToString()
    {
        return $"{Color} {Value}";
    }

    public override bool Equals(object other)
    {
        if (!(other is Card otherCard)) return false;
        return Value == otherCard.Value && Color == otherCard.Color;
    }
        
    public override int GetHashCode()
    {
        return HashCode.Combine(Color, Value);
    }

    public static Card RandomCard()
    {
        var lockObject = new object();
        int myColor;
        int myCard;
        lock (lockObject)
        {
            //0-9 is number, 10 is actioncard
            myCard = ThreadSafeRandom.ThisThreadsRandom.Next(0, 11);
            // see switch
            myColor = ThreadSafeRandom.ThisThreadsRandom.Next(1, 5);
        }

        var color = myColor switch
        {
            1 => "Red",
            2 => "Yellow",
            3 => "Green",
            _ => "Blue"
        };
            
        string value;
        if (myCard < 10)
        {
            value = myCard.ToString();
        }
        else
        {
            //4 is wild, 1-3 is action
            var action = ThreadSafeRandom.ThisThreadsRandom.Next(1, 5);
            switch (action)
            {
                case 1:
                    value = "Skip";
                    break;
                case 2:
                    value = "Reverse";
                    break;
                case 3:
                    value = "+2";
                    break;
                default:
                    var wild = ThreadSafeRandom.ThisThreadsRandom.Next(1, 3);
                    color = "Wild";
                    value = wild == 1 ? "Color" : "+4";
                    break;
            }
        }

        return new Card(color, value);
    }
        
    public static Card[] RandomCard(int count)
    {
        var cards = new Card[count];
        for (var i = 0; i < count; i++)
            cards[i] = RandomCard();
        return cards;
    }
}