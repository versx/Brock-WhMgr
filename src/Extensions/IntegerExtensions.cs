namespace WhMgr.Extensions
{
    using System;

    public static class IntegerExtensions
    {
        /*
        public static char NumberToAlphabet(this int num)
        {
            return Convert.ToChar(num + 64);
        }
        */

        public static DateTime FromUnix(this long unixSeconds)
        {
            var epochTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var localDateTime = epochTime.AddSeconds(unixSeconds);//.ToLocalTime();
            return localDateTime;
        }
    }
}