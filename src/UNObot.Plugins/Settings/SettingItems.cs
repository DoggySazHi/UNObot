using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace UNObot.Plugins.Settings
{
    public interface ISetting
    {
        [JsonProperty]
        public string JSON { get; }
        [JsonIgnore]
        public string Display { get; }
    }
    
    public class Boolean : ISetting
    {
        [JsonIgnore]
        public bool Value { get; }
        [JsonIgnore]
        public string Display => Value ? "Yes" : "No";
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(Value);
                
        public Boolean(bool value)
        {
            Value = value;
        }

        [JsonConstructor]
        public Boolean(string json)
        {
            Value = JsonConvert.DeserializeObject<bool>(json);
        }
    }
    
    public class ChannelID : ISetting
    {
        [JsonIgnore]
        public ulong ID { get; }
        [JsonIgnore]
        public string Display => ID != 0 ? $"<#{ID}>" : "(no channel set)";
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(ID);
                
        public ChannelID(ulong id)
        {
            ID = id;
        }
        
        [JsonConstructor]
        public ChannelID(string json)
        {
            ID = JsonConvert.DeserializeObject<ulong>(json);
        }
    }
    
    public class UserID : ISetting
    {
        [JsonIgnore]
        public ulong ID { get; }
        [JsonIgnore]
        public string Display => ID != 0 ? $"<@{ID}>" : "(no user set)";
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(ID);
                
        public UserID(ulong id)
        {
            ID = id;
        }
        
        [JsonConstructor]
        public UserID(string json)
        {
            ID = JsonConvert.DeserializeObject<ulong>(json);
        }
    }
    
    public class CodeBlock : ISetting
    {
        [JsonIgnore]
        public string Text { get; }
        [JsonIgnore]
        public string Display => !string.IsNullOrEmpty(Text) ? $"`{Text}`" : "`(none set)`";
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(Text);

        public CodeBlock(string text)
        {
            Text = text;
        }
        
        /// <summary>
        /// Create a setting that appears as a markdown code block.
        /// </summary>
        /// <param name="json">The JSON data to create the text.</param>
        /// <param name="unused">Only exists to distinguish it from the non-JSON version.</param>
        [JsonConstructor]
        public CodeBlock(string json, string unused)
        {
            Text = JsonConvert.DeserializeObject<string>(json);
        }
    }

    public class UserIDList : ISetting
    {
        [JsonIgnore]
        public List<UserID> UserIDs { get; }
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(UserIDs);
        [JsonProperty]
        public string Display => string.Join(", ", UserIDs);
        
        public UserIDList() : this((IEnumerable<UserID>) null) {}
        
        public UserIDList(IEnumerable<UserID> userIds)
        {
            UserIDs = userIds == null ? new List<UserID>() : new List<UserID>(userIds);
        }
        
        [JsonConstructor]
        public UserIDList(string json)
        {
            UserIDs = JsonConvert.DeserializeObject<List<UserID>>(json);
        }
    }
    
    public class ChannelIDList : ISetting
    {
        [JsonIgnore]
        public List<ChannelID> ChannelIDs { get; }
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(ChannelIDs);
        [JsonProperty]
        public string Display => ChannelIDs.Count == 0 ? "(no channels)" : string.Join(", ", ChannelIDs.Select(o => o.Display));

        public ChannelIDList() : this((IEnumerable<ChannelID>) null) {}
        
        public ChannelIDList(IEnumerable<ChannelID> channelIds)
        {
            ChannelIDs = channelIds == null ? new List<ChannelID>() : new List<ChannelID>(channelIds);
        }
        
        [JsonConstructor]
        public ChannelIDList(string json)
        {
            ChannelIDs = JsonConvert.DeserializeObject<List<ChannelID>>(json);
        }
    }
}