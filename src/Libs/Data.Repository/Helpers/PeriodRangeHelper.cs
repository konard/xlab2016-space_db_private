using Data.Repository.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Helpers
{
    public static class PeriodRangeHelper
    {
        public static List<T> SequenceWithoutGaps<T>(this List<T> source, Func<T, DateTime> keySelector, Func<DateTime, T> createForGap, Period period)
        {
            var comparisons = new Dictionary<Period, Func<DateTime, DateTime, bool>>
            {
                { Period.Yearly, (a, b) => (b - a).TotalDays < 2 * 365.2425 },
                { Period.Monthly, (a, b) => (b - a).TotalDays < 2 * 30 },
                { Period.Weekly, (a, b) => (b - a).TotalDays < 2 * 7 },
                { Period.Daily, (a, b) => (b - a).TotalDays < 2 },
            };

            var addGaps = new Dictionary<Period, Func<DateTime, DateTime>>
            {
                { Period.Yearly, _ => _.AddYears(1) },
                { Period.Monthly, _ => _.AddMonths(1) },
                { Period.Weekly, _ => _.AddDays(7) },
                { Period.Daily, _ => _.AddDays(1) },
            };

            for (var i = 0; i < source.Count - 1; i++)
            {
                var key1 = keySelector(source[i]);
                var key2 = keySelector(source[i + 1]);

                var compare = comparisons[period];

                if (!compare(key1, key2))
                {
                    var addGap = addGaps[period];
                    source.Insert(i + 1, createForGap(addGap(key1)));
                }
            }

            return source;
        }
    }
}
