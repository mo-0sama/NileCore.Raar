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

        /// <summary>
        /// Formats the DateTime as a human-readable relative time string (e.g. "just now", "5 minutes ago", "in 2 days").
        /// </summary>
        public static string ToRelativeTime(this DateTime dateTime, DateTime? relativeTo = null)
        {
            var now = relativeTo ?? (dateTime.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now);
            var ts = now - dateTime;
            bool isPast = ts.TotalMilliseconds >= 0;
            var delta = Math.Abs(ts.TotalSeconds);

            if (delta < 45)
                return isPast ? "just now" : "in moments";
            if (delta < 90)
                return isPast ? "1 minute ago" : "in 1 minute";
            if (delta < 3300)
            {
                int mins = Math.Max(1, (int)Math.Round(delta / 60.0));
                return isPast ? $"{mins} minutes ago" : $"in {mins} minutes";
            }
            if (delta < 7200)
                return isPast ? "1 hour ago" : "in 1 hour";
            if (delta < 86400)
            {
                int hours = (int)Math.Round(delta / 3600.0);
                return isPast ? $"{hours} hours ago" : $"in {hours} hours";
            }
            if (delta < 172800)
                return isPast ? "yesterday" : "tomorrow";
            if (delta < 2592000)
            {
                int days = (int)Math.Round(delta / 86400.0);
                return isPast ? $"{days} days ago" : $"in {days} days";
            }
            if (delta < 31536000)
            {
                int months = (int)Math.Round(delta / 2592000.0);
                return isPast ? $"{months} months ago" : $"in {months} months";
            }

            int years = (int)Math.Round(delta / 31536000.0);
            return isPast ? $"{years} years ago" : $"in {years} years";
        }
    }
}
