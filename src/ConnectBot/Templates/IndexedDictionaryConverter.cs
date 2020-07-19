#nullable enable
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConnectBot.Templates
{
    public class IndexedDictionaryConverter : JsonConverter
    {
        // Why did I play with generics?
        public override bool CanWrite => false;
        public override bool CanRead => true;

        // private string _typeRegex = @"\[\[(.*)\],\[(.*)\]\]";
        
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("I have never :reimuthink: ed this hard before. " +
                                                "Serialize with the default converter!");
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var json = JObject.Load(reader);
            // You can easily tell that if the database is messed up, or someone attacked it, this thing will die. Too bad!
            // var match = Regex.Match(json["$type"]!.ToString(), _typeRegex);
            // var typeA = Type.GetType(match.Groups[1].Value) ?? throw new InvalidOperationException("Cannot determine type for IndexedDictionary.");
            // var typeB = Type.GetType(match.Groups[2].Value) ?? throw new InvalidOperationException("Cannot determine type for IndexedDictionary.");
            // Kinda gave up on dynamic checking of types. Assumed ulong and int.
            var list = new IndexedDictionary<ulong, int>();
            foreach (var property in json.Properties())
            {
                if (property.Name == "$type") continue;
                var key = ulong.Parse(property.Name);
                var value = int.Parse(property.Value.ToString());
                list.Add(key, value);
            }

            return list;
        }

        public override bool CanConvert(Type objectType)
            => typeof(IndexedDictionaryConverter) == objectType;
    }
}