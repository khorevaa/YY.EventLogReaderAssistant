using System;

namespace YY.LogReader.Services
{
    internal static class IntExtensions
    {
        public static DateTime ToDateTimeFormat(this long s)
        {
            return DateTime.MinValue.AddSeconds((double)s / 10000);
        }

        public static DateTime? ToNullableDateTimeELFormat(this long s)
        {
            if (s == 0)
                return null;
            else
                return DateTime.MinValue.AddSeconds((double)s / 10000);
        }
    }
}
