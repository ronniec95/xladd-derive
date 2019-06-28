using System;
using System.Diagnostics;
using System.Threading;

namespace AARC.Utilities
{
    public static class AarcThreadLogger
    {
        public static int LogLevel { get; set; } = 4;

        /// <summary>
        /// highest priority is 0 .. e.g. 0 critical, 1 = error, 2 = warn, 3 = info, 4 = debug...?
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public static void Log(string message, int level = 3)
        {
            if (level > LogLevel)
                return;

            Debug.WriteLine($"#{Thread.CurrentThread.ManagedThreadId,3} : {DateTime.Now:HH:mm:ss.fff} : {message}");
        }
    }
}
