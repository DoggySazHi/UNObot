using System;
using Newtonsoft.Json;

namespace UNObot.Plugins.Attributes
{
    /// <summary>
    /// Annotate help information upon a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HelpAttribute : Attribute
    {
        /// <summary>
        /// Create a <see cref="HelpAttribute"/> to help aid the user understand the command they're running.
        /// </summary>
        /// <param name="usages">Usages of the command.</param>
        /// <param name="helpMsg">HelpAttribute message.</param>
        /// <param name="active">Check if command should be displayed in the help list.</param>
        /// <param name="version">Version when the command was first introduced.</param>
        [JsonConstructor]
        public HelpAttribute(string[] usages, string helpMsg, bool active, string version)
        {
            Usages = usages;
            HelpMsg = helpMsg;
            Active = active;
            Version = version;
        }

        public string[] Usages { get; }
        public string HelpMsg { get; }
        public bool Active { get; }
        public string Version { get; }
    }
}