using System;

namespace AARC.Utilities
{
    public static class DateTimeUtilities
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            //return (dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
            return (dateTime.ToLocalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static ulong ToUnsignedLong(DateTime date)
        {
            return ((ulong)date.Year) * 10000UL + (ulong)(date.Month) * 100UL + (ulong)date.Day;
        }

        public static uint ToUnsignedInt(DateTime date)
        {
            return ((uint)date.Year) * 10000U + (uint)(date.Month) * 100U + (uint)date.Day;
        }

        public static uint ToUYYYYMMDD(this DateTime date) =>((uint)date.Year) * 10000U + (uint)(date.Month) * 100U + (uint)date.Day;

        public static DateTime ToDate(ulong d)
        {
            int yyyy = (int)(d / 10000u);
            int mm = (int)((d - (uint)yyyy * 10000u) / 100);
            int dd = (int)(d - (uint)yyyy * 10000u - (uint)mm * 100u);
            return new DateTime(yyyy, mm, dd);
        }

        public static DateTime ToDate(uint d)
        {
            int yyyy = (int)(d / 10000u);
            int mm = (int)((d - (uint)yyyy * 10000u) / 100);
            int dd = (int)(d - (uint)yyyy * 10000u - (uint)mm * 100u);
            return new DateTime(yyyy, mm, dd);
        }
    }
}
