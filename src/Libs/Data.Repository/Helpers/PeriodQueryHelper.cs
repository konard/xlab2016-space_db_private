using Data.Repository.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Data.Repository.Helpers
{
    public static class PeriodQueryHelper
    {
        public static IQueryable<G> GroupBy<T, G>(this IQueryable<T> source, Dictionary<Period, Expression<Func<T, PeriodGroup>>> groupByPeriods,
            Dictionary<Period, Expression<Func<IGrouping<PeriodGroup, T>, G>>> selectByPeriods,
            Period period,
            Dictionary<Period, Expression<Func<T, bool>>> whereExpressions = null)
            where T : class
            where G : PeriodGroup
        {
            if (whereExpressions != null)
            {
                var where = whereExpressions[period];
                source = source.Where(where);
            }
            var groupByPeriod = groupByPeriods[period];
            var group = source.GroupBy(groupByPeriod);
            var selectByPeriod = selectByPeriods[period];
            return group.Select(selectByPeriod);
        }

        public static DateTime Duration(this DateTime source, Period period)
        {
            switch (period)
            {
                case Period.Yearly:
                    var time = source.AddYears(-7);
                    return new DateTime(time.Year, 1, 1);
                case Period.Monthly:
                    var time2 = source.AddYears(-1);
                    return new DateTime(time2.Year, time2.Month, 1);
                case Period.Weekly:
                    return source.AddMonths(-3);
                case Period.Daily:
                    return source.AddDays(-7);                
            }
            return source;
        }
    }
}
