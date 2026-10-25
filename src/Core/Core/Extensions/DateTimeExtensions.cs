using System;

namespace NileCore.Raar.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="DateTime"/> operations and conversions.
    /// </summary>
    public static class DateTimeExtensions
    {
        private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts a <see cref="DateTime"/> to Unix timestamp in seconds.
        /// </summary>
        public static long ToUnixTimestamp(this DateTime dateTime)
        {
            var utc = dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
            return (long)(utc - UnixEpoch).TotalSeconds;
        }

        /// <summary>
        /// Converts a Unix timestamp in seconds back to a UTC <see cref="DateTime"/>.
        /// </summary>
        public static DateTime FromUnixTimestamp(long unixTimestamp)
        {
            return UnixEpoch.AddSeconds(unixTimestamp);
        }

        /// <summary>
        /// Returns a DateTime set to the beginning of the day (00:00:00.000).
        /// </summary>
        public static DateTime StartOfDay(this DateTime dateTime)
        {
            return dateTime.Date;
        }

        /// <summary>
        /// Returns a DateTime set to the end of the day (23:59:59.999).
        /// </summary>
        public static DateTime EndOfDay(this DateTime dateTime)
        {
            return dateTime.Date.AddDays(1).AddTicks(-1);
        }

        /// <summary>
        /// Checks whether the given date falls on a weekend (Saturday or Sunday).
        /// </summary>
        public static bool IsWeekend(this DateTime dateTime)
        {
            return dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday;
        }

        /// <summary>
        /// Converts the DateTime to an ISO 8601 formatted string ("yyyy-MM-ddTHH:mm:ss.fffZ").
        /// </summary>
        public static string ToIso8601(this DateTime dateTime)
        {
            var utc = dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
            return utc.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
    }
}
