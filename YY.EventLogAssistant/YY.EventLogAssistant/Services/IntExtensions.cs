using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("YY.EventLogAssistant.Tests")]
namespace YY.EventLogAssistant.Services
{
    internal static class IntExtensions
    {
        #region #region Public Methods

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

        #endregion
    }
}
