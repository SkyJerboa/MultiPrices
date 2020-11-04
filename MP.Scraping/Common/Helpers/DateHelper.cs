using System;

namespace MP.Scraping.Common.Helpers
{
    public static class DateHelper
    {
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime ConvertEpochToDate(int seconds) => epoch.AddSeconds(seconds).ToLocalTime();
    }
}
