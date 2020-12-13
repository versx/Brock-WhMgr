﻿namespace WhMgr.Extensions
{
    using System;

    using GeoTimeZone;
    using TimeZoneConverter;

    public static class DateTimeExtensions
    {
        public static TimeSpan GetTimeRemaining(this DateTime startTime, DateTime endTime)
        {
            var remaining = TimeSpan.FromTicks(endTime.Ticks - startTime.Ticks);
            return remaining;
        }

        public static DateTime ConvertTimeFromCoordinates(this DateTime date, double lat, double lon)
        {
            var tzIana = TimeZoneLookup.GetTimeZone(lat, lon).Result;
#if Windows
            // Convert to Windows acceptable TimeZone
            tzIana = TZConvert.IanaToWindows(tzIana);
#endif
            var tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tzIana);
            var dt = DateTime.SpecifyKind(date, DateTimeKind.Utc);
            var convertedTime = TimeZoneInfo.ConvertTimeFromUtc(dt, tzInfo);
            return convertedTime;
        }

        /// <summary>
        /// Get Unix timestamp from current date time
        /// </summary>
        /// <param name="now">Date and time to get unix variation from</param>
        /// <returns>Returns Unix timestamp</returns>
        public static double GetUnixTimestamp(this DateTime now)
        {
            return now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}