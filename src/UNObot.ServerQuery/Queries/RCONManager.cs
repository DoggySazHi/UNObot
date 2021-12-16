using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using UNObot.ServerQuery.Interop;

namespace UNObot.ServerQuery.Queries;

public class RCONManager : IDisposable
{
    private List<IRCON> _reusableRCONSockets;

    public RCONManager()
    {
        _reusableRCONSockets = new List<IRCON>();
    }

    public void Dispose()
    {
        _reusableRCONSockets.ForEach(o => o.Dispose());
        _reusableRCONSockets.Clear();
        _reusableRCONSockets = null;
    }

    public IRCON GetRCON(ulong user)
    {
        var saved = _reusableRCONSockets.Where(o => o.Owner == user).ToList();
        if (saved.Count == 0) return null;
        if (!saved[0].Disposed && saved[0].Connected())
            return saved[0];
        saved[0].Dispose();
        return null;
    }

    public IRCON CreateRCON(IPEndPoint server, string password, bool reuse = false, string command = null)
    {
        IRCON rconInstance;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            rconInstance = new RCONHelper(server, password, command);
        else
            rconInstance = new RCONHelperWin(server, password, command);
        if (reuse)
            _reusableRCONSockets.Add(rconInstance);
        return rconInstance;
    }
}