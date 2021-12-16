using System;
using Newtonsoft.Json;

namespace UNObot.Plugins.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class DisableDMsAttribute : Attribute
{
    [JsonConstructor]
    public DisableDMsAttribute()
    {
        Disabled = true;
    }

    [JsonConstructor]
    public DisableDMsAttribute(bool disabled)
    {
        Disabled = disabled;
    }

    public bool Disabled { get; }

    public bool Enabled => !Disabled;
}