using System;
using System.Collections.Generic;

namespace Data.Repository.Helpers
{
    public static class DateTimeHelper
    {
        public static DateTime ToUtc(this DateTime source)
        {
            if (source.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(source, DateTimeKind.Utc);

            return source;
        }

        public static List<DateTime> RangeByDays(this DateTime start, DateTime end)
        {
            var result = new List<DateTime>();

            for (var i = start; i < end; i = i.AddDays(1))
                result.Add(i);

            return result;
        }
    }
}
