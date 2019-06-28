using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AARC.Utilities
{
    public class AarcTimer
    {
        public static void CompareMethods(Func<object, object>[] fns, int numTrials = 200)
        {
            List<long> timesInMs = fns.Select(fn => TimeMethod(numTrials, fn, null)).ToList();
            string s = timesInMs.Select((x, i) => i.ToString() + "=" + x.ToString() + "ms").ToCsv();

            Console.WriteLine("Times over {0} trials: {1}", numTrials, s);
        }

        public static long TimeMethod(int times, Action<object> action, object param)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Enumerable.Repeat(0, times).ForEach(x => action(param));
            timer.Stop();

            return timer.ElapsedMilliseconds;
        }

        public static long TimeMethod(int times, Func<object, object> function, object param)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Enumerable.Repeat(0, times).ForEach(x => function(param));
            timer.Stop();

            return timer.ElapsedMilliseconds;
        }

        public static void DisplayTimerProperties()
        {
            // Display the timer frequency and resolution. 
            if (Stopwatch.IsHighResolution)
            {
                Console.WriteLine("Operations timed using the system's high-resolution performance counter.");
            }
            else
            {
                Console.WriteLine("Operations timed using the DateTime class.");
            }

            long frequency = Stopwatch.Frequency;
            Console.WriteLine("  Timer frequency in ticks per second = {0}",
                frequency);
            long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;
            Console.WriteLine("  Timer is accurate within {0} nanoseconds",
                nanosecPerTick);
        }

        public static void HashTest()
        {
            System.Collections.Hashtable hs = new System.Collections.Hashtable();
            List<string> sc = new List<string>();

            for (int i = 0; i < int.MaxValue / 1000; i++)
            {
                hs.Add(i.ToString(), i);
                sc.Add(i.ToString());
            }

            Random rnd = new Random();
            Stopwatch timer = new Stopwatch();

            int val = rnd.Next(int.MaxValue / 1000);

            timer.Start();
            if (sc.Contains(val.ToString()))
            {
                Console.WriteLine("Contains");
            }
            timer.Stop();

            Console.WriteLine(timer.ElapsedTicks);
            timer.Reset();

            timer.Start();
            if (hs.Contains(val.ToString()))
            {
                Console.WriteLine("Contains");
            }
            timer.Stop();

            Console.WriteLine(timer.ElapsedTicks);
        }

        public static void CompareTimes(long[] times1, long[] times2)
        {
            // e.g. times as ElapsedMilliseconds

            int numRuns = times1.Length;

            // Sort the elapsed times for all test runs
            Array.Sort(times1);
            Array.Sort(times2);

            // Calculate the total times discarding
            // the 5% min and 5% max test times
            long time1 = 0, time2 = 0;
            int discardCount = (int)Math.Round(numRuns * 0.05);
            int count = numRuns - discardCount;
            for (int i = discardCount; i < count; i++)
            {
                time1 += times1[i];
                time2 += times2[i];
            }

            // format results..
            // Add the times to the string
            string m_Name1 = "Method1";
            string m_Name2 = "Method2";

            int nameLength = Math.Max(m_Name1.Length, m_Name2.Length);
            int timeLength = Math.Max(time1.ToString().Length,
                time2.ToString().Length);

            string result = string.Format("{0," + nameLength + "} = {1," +
                timeLength + "} ms\n{2," + nameLength + "} = {3," +
                timeLength + "} ms\n------------------------------\n",
                m_Name1, time1, m_Name2, time2);

            // Determine who's faster
            if (time1 != time2)
            {
                result += string.Format("{0} is {1:P} faster",
                    time1 < time2 ? m_Name1 : m_Name2,
                    (double)Math.Max(time1, time2) / Math.Min(time1, time2) - 1);
            }
            else
            {
                result += "The times are equal";
            }

            Console.WriteLine(result);
        }

    }
}
