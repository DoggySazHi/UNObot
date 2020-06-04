using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using UNObot.Services;
using static UNObot.Services.IRCON;

namespace UNObot.Interop
{
    public class RCONHelper : IRCON
    {
        private IntPtr RCONInstance;
        public bool Disposed { get; private set; }

        [DllImport(@"libRCONHelper.so")]
        private static extern IntPtr CreateObjectA(string ip, ushort port, string password, string command);

        [DllImport(@"libRCONHelper.so")]
        private static extern IntPtr CreateObjectB(string ip, ushort port, string password);

        [DllImport(@"libRCONHelper.so")]
        private static extern void DestroyObject(IntPtr RCON);

        [DllImport(@"libRCONHelper.so")]
        private static extern void Mukyu(IntPtr RCON);

        [DllImport(@"libRCONHelper.so")]
        public static extern void MukyuN();

        [DllImport(@"libRCONHelper.so")]
        public static extern IntPtr Say(string Thing);

        [DllImport(@"libRCONHelper.so")]
        public static extern void SayDelete(IntPtr Thing);

        [DllImport(@"libRCONHelper.so")]
        private static extern RCONStatus GetStatus(IntPtr obj);

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

        /*
        [DllImport(@"libRCONHelper.so")]
        private static extern void Dispose(IntPtr obj);
        
        [DllImport(@"libRCONHelper.so")]
        private static extern bool Disposed(IntPtr obj);
        */

        public ulong Owner { get; set; }
        public string Data => Marshal.PtrToStringAnsi(GetData(RCONInstance));
        public RCONStatus Status => GetStatus(RCONInstance);
        public IPEndPoint Server => new IPEndPoint(IPAddress.Parse(Marshal.PtrToStringAnsi(GetServerIP(RCONInstance)) ?? throw new InvalidOperationException("OH CRAP")), GetServerPort(RCONInstance));

        public RCONHelper(IPEndPoint Server, [NotNull] string password, string command = null)
        {
            // Note: It is split into two parts as C++ does not allow null strings.
            RCONInstance = command switch
            {
                null => CreateObjectB(Server.Address.ToString(), (ushort)Server.Port, password),
                _ => CreateObjectA(Server.Address.ToString(), (ushort)Server.Port, password, command)
            };
        }

        public RCONHelper([NotNull] string ip, ushort port, [NotNull] string password, string command = null)
        {
            // Note: It is split into two parts as C++ does not allow null strings.
            RCONInstance = command switch
            {
                null => CreateObjectB(ip, port, password),
                _ => CreateObjectA(ip, port, password, command)
            };
        }

        public void Mukyu()
        {
            CheckDispose();
            // to mukyu, mukyu (test to check interop)
            Mukyu(RCONInstance);
        }

        public void Execute(string Command, bool Reuse = false)
        {
            CheckDispose();
            Execute(RCONInstance, Command);
            if (!Reuse)
                Dispose();
        }

        public void ExecuteSingle(string Command, bool Reuse = false)
        {
            CheckDispose();
            ExecuteSingle(RCONInstance, Command);
            if (!Reuse)
                Dispose();
        }

        public bool Connected()
        {
            CheckDispose();
            return Connected(RCONInstance);
        }

        private void CheckDispose()
        {
            if (Disposed)
                throw new ObjectDisposedException("libRCONHelper", "Attempted to dispose of an object that was already disposed! " +
                    "This is a native object; attempting to re-dispose of it will cause a segfault!");
        }

        public void Dispose()
        {
            if (Disposed) return;
            DestroyObject(RCONInstance);
            Disposed = true;
        }
    }
}