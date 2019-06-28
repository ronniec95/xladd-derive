using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AARC.Utilities;

namespace AARC.DateLib
{
    public class DateNames
    {
        public static int[] GetQuarter(int month)
        {
            int before = month == 1 ? 12 : month - 1;
            int after = month == 12 ? 1 : month + 1;

            return new int[] { before, month, after };
        }

        public static string GetQuarterName(int month)
        {
            return GetQuarter(month).Select(GetMonthName).ToCsv().Replace(',', '-');
        }

        public static string GetMonthName(int month)
        {
            switch (month)
            {
                case 1:
                    return "Jan";
                case 2:
                    return "Feb";
                case 3:
                    return "Mar";
                case 4:
                    return "Apr";
                case 5:
                    return "May";
                case 6:
                    return "Jun";
                case 7:
                    return "Jul";
                case 8:
                    return "Aug";
                case 9:
                    return "Sep";
                case 10:
                    return "Oct";
                case 11:
                    return "Nov";
                case 12:
                    return "Dec";
                default:
                    return "";
            }
        }

    }
}
