using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.InteropServices;
using UNObot.Services;

namespace UNObot.Interop
{
    public class RCONHelper : IDisposable
    {
        private IntPtr RCONInstance;
        
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
        private static extern MinecraftRCON.RCONStatus Status(IntPtr obj);
        
        [DllImport(@"libRCONHelper.so")]
        private static extern bool Disposed(IntPtr obj);
        
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
        
        [DllImport(@"libRCONHelper.so")]
        private static extern void Dispose(IntPtr obj);

        public string Data => Marshal.PtrToStringAnsi(GetData(RCONInstance));
        public IPEndPoint Server => new IPEndPoint(IPAddress.Parse(Marshal.PtrToStringAnsi(GetServerIP(RCONInstance)) ?? throw new InvalidOperationException("OH CRAP")), GetServerPort(RCONInstance));

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
            // to mukyu, mukyu (test to check interop)
            Mukyu(RCONInstance);
        }

        public void Execute(string Command)
        {
            Execute(RCONInstance, Command);
        }
        
        public void ExecuteSingle(string Command)
        {
            ExecuteSingle(RCONInstance, Command);
        }
        
        public bool Connected()
        {
            return Connected(RCONInstance);
        }

        public void Dispose()
        {
            Dispose(RCONInstance);
        }
    }
}