using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace UNObot.Interop
{
    public class RCONHelper
    {
        private IntPtr RCONInstance;
        
        [DllImport(@"RCONHelper.dll")]
        private static extern IntPtr CreateObjectA(ref string ip, ref string password, bool reuse, ref string command);
        
        [DllImport(@"RCONHelper.dll")]
        private static extern IntPtr CreateObjectB(ref string ip, ref string password, bool reuse);
        
        [DllImport(@"RCONHelper.dll")]
        private static extern IntPtr CreateObjectC(ref string ip, ref string password);
        
        [DllImport(@"RCONHelper.dll")]
        private static extern void DestroyObject(IntPtr RCON);
        
        [DllImport(@"RCONHelper.dll")]
        private static extern void Mukyu(IntPtr RCON);
        
        ~RCONHelper()
        {
            DestroyObject(RCONInstance);
        }

        public RCONHelper([NotNull] string ip, [NotNull] string password, bool reuse = false, string command = null)
        {
            // Note: It is split into three parts as C++ does not allow null strings.
            RCONInstance = command switch
            {
                null when reuse == false => CreateObjectC(ref ip, ref password),
                null => CreateObjectB(ref ip, ref password, true),
                _ => CreateObjectA(ref ip, ref password, reuse, ref command)
            };
        }

        public void Mukyu()
        {
            // to mukyu, mukyu
            Mukyu(RCONInstance);
        }
    }
}