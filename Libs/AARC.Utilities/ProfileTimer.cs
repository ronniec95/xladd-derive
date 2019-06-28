using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AARC.Utilities
{
    public class ProfileTimer
    {
        public class ProfileTimerInfo
        {
            public long TimesCalled;

            public Stopwatch SW;
        }

        public static Dictionary<string, ProfileTimerInfo> Info = new Dictionary<string, ProfileTimerInfo>();

        public static void Reset()
        {
            Info.Clear();
        }

        public static void Restart(string name)
        {
            if (Info.ContainsKey(name))
            {
                Info[name].SW.Restart();
            }
            else
            {
                throw new Exception("Restarting " + name + " but it was never started");
            }
        }

        public static void Start(string name)
        {
            if (Info.ContainsKey(name))
            {
                Debug.Assert(!Info[name].SW.IsRunning, "Restarting " + name + " but it is already running");
                Info[name].SW.Start();
            }
            else
            {
                ProfileTimerInfo pti = new ProfileTimerInfo();
                pti.SW = Stopwatch.StartNew();
                Info.Add(name, pti);
            }
        }

        public static void Stop(string name)
        {
            Debug.Assert(Info.ContainsKey(name), "Tried to stop " + name + " but it has not been started");
            if (!Info.ContainsKey(name))
                return;

            Debug.Assert(Info[name].SW.IsRunning, "Stopping " + name + " but it is not running");

            Info[name].SW.Stop();
            Info[name].TimesCalled++;
        }

        public static void DebugDisplay(string name)
        {
            Debug.WriteLine($"{name} Times={Info[name].TimesCalled} Elapsed={Info[name].SW.ElapsedMilliseconds}ms");
        }
    }
}
