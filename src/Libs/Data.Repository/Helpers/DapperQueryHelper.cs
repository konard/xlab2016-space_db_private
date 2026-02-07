using Data.Repository.Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Data.Repository.Helpers
{
    public static class DapperQueryHelper
    {
        public static async Task<PagedList<T>> SearchPageAsync<T, TFilter, TSort>(this IDapperDbContext source,
            string tableName,
            QueryBase<T, TFilter, TSort> query)
            where TFilter : FilterBase<T>
            where TSort : SortBase<T>
        {
            return await SearchPageAsync(source, tableName, query.Paging, query.Filter, query.Sort, 
                query.FilterOperator);
        }

        public static async Task<PagedList<T>> SearchPageAsync<T>(this IDapperDbContext source, 
            string tableName, Paging paging, IFilter filter, SortBase<T> sort, FilterOperator? filterOperator)
        {
            int? count = paging?.ReturnCount == true ?
                await source.CountByTableNameAsync(tableName) : null;
            var list = await SearchAsync(source, tableName, paging, filter, sort, filterOperator);

            return new PagedList<T>(list, count, paging);
        }

        public static async Task<IEnumerable<T>> SearchAsync<T>(this IDapperDbContext source, 
            string tableName, Paging paging, IFilter queryFilter, SortBase<T> querySort, FilterOperator? filterOperator)
        {
            var limit = string.Empty;
            var offset = string.Empty;

            if (paging != null)
            {
                if (paging.Skip.HasValue)
                {
                    offset = $"offset {paging.Skip}";
                }

                if (paging.Take.HasValue)
                {
                    limit = $"limit {paging.Take}";
                }
            }

            return await source.ToListAsync<T>($@"
                select t.*
                from ""{tableName}"" as t                
                /**where**/
                /**orderby**/
                {limit}
                {offset}
            ", (_) =>
            {
                if (querySort != null)
                {
                    var sort = QueryHelper.ExpandSort(querySort).
                        OrderBy(_ => _.Value.Ordinal).ToList();

                    foreach (var item in sort)
                    {
                        var sortOperator = item.Value.Operator == SortOperator.Desc ? " desc" : string.Empty;
                        _.OrderBy(@$"t.""{item.Key}""{sortOperator}");
                    }
                }

                if (queryFilter != null)
                {
                    var filter = QueryHelper.ExpandFilter2(queryFilter);
                    DapperFilterHelper.Filter(_, "t", filter, filterOperator);
                }
            });
        }

        public static async Task<T> FindWhereColumnValueEqualsAsync<T>(this IDapperDbContext source,
            string tableName, string columnName, object value, Func<string, string> columnExpression = null)
        {
            var whereColumnName = columnExpression != null ? columnExpression(columnName) : $"t.\"{columnName}\"";

            return await source.Find<T>(@$"
                select t.*
                from ""{tableName}"" as t
                /**where**/", where: (_) =>
            {
                _.Where(@$"{whereColumnName} = @{columnName}", DynamicHelper.CreateDynamic((_) =>
                {
                    _.TryAdd(columnName, value);
                }));
            });
        }
    }
}
