using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Helpers
{
    public static class QueryHelper
    {
        public static async Task<PagedList<T>> SearchPageAsync<T, TFilter, TSort>(this IQueryable<T> source,
            QueryBase<T, TFilter, TSort> query)
            where TFilter : FilterBase<T>
            where TSort : SortBase<T>
        {
            return await SearchPageAsync(source, query.Paging, query.Filter, query.Sort, query.FilterOperator);
        }

        private static async Task<PagedList<T>> SearchPageAsync<T>(this IQueryable<T> source,
            Paging paging, IFilter filter, SortBase<T> sort, FilterOperator? filterOperator)
        {
            if (sort != null)
            {
                var sortDescription = ExpandSort(sort);

                for (var i = 0; i < sortDescription.Count; i++)
                {
                    var item = sortDescription[i];
                    source = SortHelper.Sort(source, item.Key, item.Value, i > 0);
                }
            }

            if (filter != null)
            {
                source = FilterHelper.Filter(source, filter, filterOperator);
            }

            int? count = paging?.ReturnCount == true ?
                await source.CountAsync() : null;

            source = source.ByQueryPaging(paging);

            var list = await source.ToListAsync();

            return new PagedList<T>(list, count, paging);
        }

        public static Dictionary<string, FilterOperandExpression> ExpandFilter<T>(FilterBase<T> filter)
        {
            return ExpandFilter2(filter);
        }

        public static Dictionary<string, FilterOperandExpression> ExpandFilter2(IFilter filter)
        {
            var type = filter.GetType();
            var result = filter.GetType().GetProperties().ToDictionary(_ => _.Name, _ =>
            {
                var value = _.GetValue(filter);

                if (value == null)
                    return null;

                var valueProperties = value.GetType().GetProperties();

                if (value is IFilter || value is IAdvancedFilter)
                {
                    return new FilterOperandExpression
                    {
                        Operand1 = value,
                        Type = value.GetType()
                    };
                }
                else
                {
                    var operand1Property = valueProperties.First(_ => _.Name == "Operand1");
                    var operand2Property = valueProperties.First(_ => _.Name == "Operand2");
                    var operatorProperty = valueProperties.First(_ => _.Name == "Operator");

                    var operand1 = operand1Property.GetValue(value);
                    var operand2 = operand2Property.GetValue(value);

                    if (operand1 != null && operand1 is DateTime time1)
                        operand1 = time1.ToUtc();

                    if (operand2 != null && operand2 is DateTime time2)
                        operand2 = time2.ToUtc();

                    return new FilterOperandExpression
                    {
                        Operand1 = operand1,
                        Operand2 = operand2,
                        Operator = (FilterOperator)operatorProperty.GetValue(value),
                        Type = operand1Property.PropertyType
                    };
                }
            });

            return result.Where(_ => _.Value != null).ToDictionary(_ => _.Key, _ => _.Value);
        }

        public static List<KeyValuePair<string, SortOperand>> ExpandSort<T>(SortBase<T> sort)
        {
            var type = sort.GetType();
            return type.GetProperties().Select(_ =>
                    new KeyValuePair<string, SortOperand>(_.Name, _.GetValue(sort) as SortOperand)).
                Where(_ => _.Value != null).
                OrderBy(_ => _.Value.Ordinal ?? 0).ToList();
        }

        public static async Task BatchOperationAsync<T>(this IQueryable<T> source,
            Func<IQueryable<T>, Task> operation,
            int batchCount, int? batchDelay = null)
        {
            var count = await source.CountAsync();

            for (var i = 0; i < count; i += batchCount)
            {
                var batch = source.Skip(i).Take(batchCount);
                await operation(batch);

                if (batchDelay.HasValue)
                    await Task.Delay(batchDelay.Value);
            }
        }

        public static void BatchOperation<T>(this IQueryable<T> source,
            Action<IQueryable<T>> operation,
            int batchCount, int? batchDelay = null)
        {
            var count = source.Count();

            for (var i = 0; i < count; i += batchCount)
            {
                var batch = source.Skip(i).Take(batchCount);
                operation(batch);

                if (batchDelay.HasValue)
                    Task.Delay(batchDelay.Value).Wait();
            }
        }
    }
}
