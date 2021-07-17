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
        [JsonIgnore] private readonly bool _value;
        [JsonIgnore]
        public string Display => _value ? "Yes" : "No";
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(_value);
                
        public Boolean(bool value)
        {
            _value = value;
        }

        [JsonConstructor]
        public Boolean(string json)
        {
            _value = JsonConvert.DeserializeObject<bool>(json);
        }
    }
    
    public class ChannelID : ISetting
    {
        [JsonIgnore] private readonly ulong _id;
        [JsonIgnore]
        public string Display => _id != 0 ? $"<#{_id}>" : "(no channel set)";
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(_id);
                
        public ChannelID(ulong id)
        {
            _id = id;
        }
        
        [JsonConstructor]
        public ChannelID(string json)
        {
            _id = JsonConvert.DeserializeObject<ulong>(json);
        }
    }
    
    public class UserID : ISetting
    {
        [JsonIgnore] private readonly ulong _id;
        [JsonIgnore]
        public string Display => _id != 0 ? $"<@{_id}>" : "(no user set)";
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(_id);
                
        public UserID(ulong id)
        {
            _id = id;
        }
        
        [JsonConstructor]
        public UserID(string json)
        {
            _id = JsonConvert.DeserializeObject<ulong>(json);
        }
    }
    
    public class CodeBlock : ISetting
    {
        [JsonIgnore] private readonly string _text;
        [JsonIgnore]
        public string Display => !string.IsNullOrEmpty(_text) ? $"`{_text}`" : "`(none set)`";
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(_text);

        public CodeBlock(string text)
        {
            _text = text;
        }
        
        /// <summary>
        /// Create a setting that appears as a markdown code block.
        /// </summary>
        /// <param name="json">The JSON data to create the text.</param>
        /// <param name="unused">Only exists to distinguish it from the non-JSON version.</param>
        [JsonConstructor]
        public CodeBlock(string json, string unused)
        {
            _text = JsonConvert.DeserializeObject<string>(json);
        }
    }

    public class UserIDList : ISetting
    {
        [JsonIgnore] private List<UserID> _userIDs;
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(_userIDs);
        [JsonProperty]
        public string Display => string.Join(", ", _userIDs);
        
        public UserIDList() : this((IEnumerable<UserID>) null) {}
        
        public UserIDList(IEnumerable<UserID> userIds)
        {
            _userIDs = userIds == null ? new List<UserID>() : new List<UserID>(userIds);
        }
        
        [JsonConstructor]
        public UserIDList(string json)
        {
            _userIDs = JsonConvert.DeserializeObject<List<UserID>>(json);
        }
    }
    
    public class ChannelIDList : ISetting
    {
        [JsonIgnore] private readonly List<ChannelID> _channelIds;

        [JsonProperty] public string JSON => JsonConvert.SerializeObject(_channelIds);

        [JsonProperty]
        public string Display => _channelIds.Count == 0 ? "(no channels)" : string.Join(", ", _channelIds.Select(o => o.Display));

        public ChannelIDList() : this((IEnumerable<ChannelID>) null) {}
        
        public ChannelIDList(IEnumerable<ChannelID> channelIds)
        {
            _channelIds = channelIds == null ? new List<ChannelID>() : new List<ChannelID>(channelIds);
        }
        
        [JsonConstructor]
        public ChannelIDList(string json)
        {
            _channelIds = JsonConvert.DeserializeObject<List<ChannelID>>(json);
        }
    }
}