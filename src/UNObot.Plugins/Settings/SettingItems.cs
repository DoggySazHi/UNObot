using System.Collections.Generic;
using Newtonsoft.Json;

namespace UNObot.Plugins.Settings
{
    public interface ISetting
    {
        [JsonProperty]
        public string JSON { get; }
        [JsonProperty]
        public string Display { get; }
    }
    
    public class Boolean : ISetting
    {
        [JsonIgnore] private bool Value { get; }
        [JsonProperty]
        public string Display => Value ? "Yes" : "No";
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(Value);
                
        public Boolean(bool value)
        {
            Value = value;
        }
    }
    
    public class ChannelID : ISetting
    {
        [JsonIgnore] private ulong Id { get; }
        [JsonProperty]
        public string Display => Id != 0 ? $"<#{Id}>" : "(none set)";
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(Id);
                
        public ChannelID(ulong id)
        {
            Id = id;
        }
    }
    
    public class UserID : ISetting
    {
        [JsonIgnore] private ulong Id { get; }
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(Id);
        [JsonProperty]
        public string Display => Id != 0 ? $"<@{Id}>" : "(none set)";
                
        public UserID(ulong id)
        {
            Id = id;
        }
    }
    
    public class CodeBlock : ISetting
    {
        [JsonIgnore] private string Text { get; }
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(Text);
        [JsonProperty]
        public string Display => !string.IsNullOrEmpty(Text) ? $"`{Text}`" : "`(none set)`";
        
        public CodeBlock(string text)
        {
            Text = text;
        }
    }

    public class UserIDList : ISetting
    {
        [JsonIgnore] private List<UserID> UserIds { get; }
        [JsonProperty] public string JSON => JsonConvert.SerializeObject(UserIds);
        [JsonProperty]
        public string Display => string.Join(", ", UserIds);
        
        public UserIDList() : this(null) {}
        
        public UserIDList(IEnumerable<UserID> userIds)
        {
            UserIds = userIds == null ? new List<UserID>() : new List<UserID>(userIds);
        }
    }
    
    public class ChannelIDList : ISetting
    {
        [JsonIgnore] private readonly List<ChannelID> _channelIds;

        [JsonProperty] public string JSON => JsonConvert.SerializeObject(_channelIds);

        [JsonProperty]
        public string Display => string.Join(", ", _channelIds);

        public ChannelIDList() : this(null) {}
        
        public ChannelIDList(IEnumerable<ChannelID> channelIds)
        {
            _channelIds = channelIds == null ? new List<ChannelID>() : new List<ChannelID>(channelIds);
        }
    }
}