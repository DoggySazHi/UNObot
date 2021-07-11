using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using UNObot.ServerQuery.Queries;

namespace UNObot.ServerQuery.Interop
{
    public class RCONHelper : IRCON
    {
        private readonly IntPtr _rconInstance;

        public RCONHelper(IPEndPoint server, [NotNull] string password, string command = null)
        {
            // Note: It is split into two parts as C++ does not allow null strings.
            _rconInstance = command switch
            {
                null => CreateObjectB(server.Address.ToString(), (ushort) server.Port, password),
                _ => CreateObjectA(server.Address.ToString(), (ushort) server.Port, password, command)
            };
        }

        public RCONHelper([NotNull] string ip, ushort port, [NotNull] string password, string command = null)
        {
            // Note: It is split into two parts as C++ does not allow null strings.
            _rconInstance = command switch
            {
                null => CreateObjectB(ip, port, password),
                _ => CreateObjectA(ip, port, password, command)
            };
        }

        public bool Disposed { get; private set; }

        /*
        [DllImport(@"libRCONHelper.so")]
        private static extern void Dispose(IntPtr obj);
        
        [DllImport(@"libRCONHelper.so")]
        private static extern bool Disposed(IntPtr obj);
        */

        public ulong Owner { get; set; }
        public string Data => Marshal.PtrToStringAnsi(GetData(_rconInstance));
        public IRCON.RCONStatus Status => GetStatus(_rconInstance);

        public IPEndPoint Server =>
            new(
                IPAddress.Parse(Marshal.PtrToStringAnsi(GetServerIP(_rconInstance)) ??
                                throw new InvalidOperationException("OH CRAP")), GetServerPort(_rconInstance));

        public void Execute(string command, bool reuse = false)
        {
            CheckDispose();
            Execute(_rconInstance, command);
            if (!reuse)
                Dispose();
        }

        public void ExecuteSingle(string command, bool reuse = false)
        {
            CheckDispose();
            ExecuteSingle(_rconInstance, command);
            if (!reuse)
                Dispose();
        }

        public bool Connected()
        {
            CheckDispose();
            return Connected(_rconInstance);
        }

        public void Dispose()
        {
            if (Disposed) return;
            DestroyObject(_rconInstance);
            Disposed = true;
        }

        [DllImport(@"libRCONHelper.so")]
        private static extern IntPtr CreateObjectA(string ip, ushort port, string password, string command);

        [DllImport(@"libRCONHelper.so")]
        private static extern IntPtr CreateObjectB(string ip, ushort port, string password);

        [DllImport(@"libRCONHelper.so")]
        private static extern void DestroyObject(IntPtr rcon);

        [DllImport(@"libRCONHelper.so")]
        private static extern void Mukyu(IntPtr rcon);

        [DllImport(@"libRCONHelper.so")]
        public static extern void MukyuN();

        [DllImport(@"libRCONHelper.so")]
        public static extern IntPtr Say(string thing);

        [DllImport(@"libRCONHelper.so")]
        public static extern void SayDelete(IntPtr thing);

        [DllImport(@"libRCONHelper.so")]
        private static extern IRCON.RCONStatus GetStatus(IntPtr obj);

        [DllImport(@"libRCONHelper.so")]
        private static extern IntPtr GetData(IntPtr obj);

        [DllImport(@"libRCONHelper.so")]
        private static extern IntPtr GetServerIP(IntPtr obj);

        [DllImport(@"libRCONHelper.so")]
        private static extern ushort GetServerPort(IntPtr obj);

        [DllImport(@"libRCONHelper.so")]
        private static extern bool Connected(IntPtr obj);

        [DllImport(@"libRCONHelper.so")]
        private static extern void ExecuteSingle(IntPtr obj, string command);

        [DllImport(@"libRCONHelper.so")]
        private static extern void Execute(IntPtr obj, string command);

        public void Mukyu()
        {
            CheckDispose();
            // to mukyu, mukyu (test to check interop)
            Mukyu(_rconInstance);
        }

        private void CheckDispose()
        {
            if (Disposed)
                throw new ObjectDisposedException("libRCONHelper",
                    "Attempted to dispose of an object that was already disposed! " +
                    "This is a native object; attempting to re-dispose of it will cause a segfault!");
        }
    }
}