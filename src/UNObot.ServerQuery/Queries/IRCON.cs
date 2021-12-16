using System;
using System.Net;

namespace UNObot.ServerQuery.Queries;

public interface IRCON : IDisposable
{
    public enum RCONStatus
    {
        ConnFail,
        AuthFail,
        ExecFail,
        IntFail,
        Success
    }

    public ulong Owner { get; set; }
    public IPEndPoint Server { get; }
    public RCONStatus Status { get; }
    public string Data { get; }
    public bool Disposed { get; }
    public void Execute(string command, bool reuse = false);
    public void ExecuteSingle(string command, bool reuse = false);
    public bool Connected();
}