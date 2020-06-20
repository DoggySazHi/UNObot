using System;
using System.Collections.Generic;
using System.Threading;

namespace UNObot.TerminalCore
{
    internal static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random _local;

        internal static Random ThisThreadsRandom =>
            _local ??= new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId));

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = ThisThreadsRandom.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}