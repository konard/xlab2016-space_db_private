using Data.Repository.Stability;
using System;
using System.Linq;
using System.Linq.Expressions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Threading.Tasks;
using Data.Repository.Dapper;
using Microsoft.EntityFrameworkCore;

namespace Data.Repository.Helpers
{
    public static class FilterHelper
    {
        // TODO: implement QueryBuilder
        public static IQueryable<T> Filter<T>(IQueryable<T> source, IFilter filter, FilterOperator? filterOperator = null)
        {
            var _ = Expression.Parameter(typeof(T), "_");
            var memberAccessor = (Expression)_;

            var expression = Filter<T>(null, memberAccessor, filter, filterOperator);

            if (expression == null)
            {
                return source;
            }

            var lambda = Expression.Lambda<Func<T, bool>>(expression, _);
            return source.Where(lambda);
        }

        public static Expression Filter<T>(Expression expression, 
            Expression memberAccessor, IFilter filter, FilterOperator? filterOperator = null)
        {            
            var statements = QueryHelper.ExpandFilter2(filter);

            foreach (var statementKey in statements.Keys)
            {
                var memberKey = statementKey;
                var statement = statements[statementKey];

                Expression operation;
                if (statement.Operand1 is IAdvancedFilter)
                    continue;
                if (statement.Operand1 is IFilter value)
                {                    
                    var memberAccessor2 = Expression.Property(memberAccessor, memberKey);
                    operation = Filter<T>(expression, memberAccessor2, value, filterOperator);
                }
                else
                {
                    operation = Filter(memberAccessor, memberKey,
                        statement.Operand1,
                        statement.Operand2,
                        statement.Type, statement.Operator, filterOperator);
                }

                if (expression == null)
                {
                    expression = operation;
                }
                else
                {
                    expression = filterOperator == FilterOperator.Or ?
                        Expression.OrElse(expression, operation) :
                        Expression.AndAlso(expression, operation);                    
                }
            }

            return expression;
        }

        public static IQueryable<T> Filter<T>(IQueryable<T> source, string key,
            FilterOperandExpression filterOperandExpression, FilterOperator? filterOperator = null)
        {
            //if (filterOperandExpression.Operand1 == null)
            //    return source;

            var _ = Expression.Parameter(typeof(T), "_");
            var memberAccessor = (Expression)_;

            Expression operation = null;

            if (filterOperandExpression.Operand1 is IFilter value)
            {
                memberAccessor = Expression.Property(memberAccessor, key);
                var tokens = QueryHelper.ExpandFilter2(value);

                foreach (var tokenKey in tokens.Keys)
                {
                    var token = tokens[tokenKey];
                    var operation2 = Filter(memberAccessor, tokenKey, 
                        token.Operand1, token.Operand2, 
                        token.Type, token.Operator, filterOperator);
                    if (operation == null)
                    {
                        operation = operation2;
                    }
                    else
                    {                        
                        operation = filterOperator == FilterOperator.Or ?
                            Expression.OrElse(operation, operation2) :
                            Expression.AndAlso(operation, operation2);
                    }                    
                }
            }
            else
            {
                operation = Filter(memberAccessor, key,
                    filterOperandExpression.Operand1,
                    filterOperandExpression.Operand2,
                    filterOperandExpression.Type, filterOperandExpression.Operator, filterOperator);
            }

            if (operation == null)
                return source;

            var lambda = Expression.Lambda<Func<T, bool>>(operation, _);
            return source.Where(lambda);
        }

        public static Expression Filter(Expression memberAccessor, string key,
            object operand1Value,
            object operand2Value, Type operandType,
            FilterOperator @operator, FilterOperator? combineOperator)
        {
            var property = Expression.Property(memberAccessor, key);
            var operand1 = Expression.Constant(operand1Value, operandType);

            Expression result = null;

            switch (@operator)
            {
                case FilterOperator.Equals:
                    result = Expression.Equal(property, operand1);
                    break;
                case FilterOperator.GreaterThan:
                    result = Expression.GreaterThan(property, operand1);
                    break;
                case FilterOperator.GreaterThanOrEqual:
                    result = Expression.GreaterThanOrEqual(property, operand1);
                    break;
                case FilterOperator.LessThan:
                    result = Expression.LessThan(property, operand1);
                    break;
                case FilterOperator.LessThanOrEqual:
                    result = Expression.LessThanOrEqual(property, operand1);
                    break;
                case FilterOperator.Like:
                    result = Expression.Call(property, typeof(string).GetMethod("Contains", new[] { typeof(string) }), operand1);
                    break;
                case FilterOperator.StartsWith:
                    result = Expression.Call(property, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), operand1);
                    break;
                case FilterOperator.EndsWith:
                    result = Expression.Call(property, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), operand1);
                    break;
                case FilterOperator.Between:                    
                    var operand2 = Expression.Constant(operand2Value, operandType);

                    Expression operation1 = null;
                    Expression operation2 = null;

                    if (operand1Value != null)
                        operation1 = Expression.LessThanOrEqual(operand1, property);

                    if (operand2Value != null)
                        operation2 = Expression.LessThanOrEqual(property, operand2);

                    if (operand2Value == null)
                        result = operation1;
                    else if (operand1Value == null)
                    {
                        result = operation2;
                    }
                    else
                        result = Expression.AndAlso(operation1, operation2);
                    break;
            }

            return result;
        }

        public static async Task<PagedList<T>> SearchUsingDapperAsync<T, TQuery>(IQueryBase<T> query,
            IDapperDbContext restDapperDb, string tableName)
        {
            return await RetryHelper.RetryDbAsAsync(async () =>
            {
                return await restDapperDb.SearchPageAsync(tableName, query.Paging, query.Filter, query.Sort, query.FilterOperator);
            });
        }

        public static async Task<PagedList<T>> SearchUsingEfAsync<T, TFilter, TSort>(QueryBase<T, TFilter, TSort> query,
            Func<IQueryable<T>, IQueryable<T>> expression, DbContext restDb, IQueryable<T> restSource = null)
            where T : class
            where TFilter : FilterBase<T>
            where TSort : SortBase<T>
        {
            return await RetryHelper.RetryDbAsAsync(async () =>
            {
                var source = (restSource ?? restDb.Set<T>()).AsNoTracking();

                if (expression != null)
                    source = expression(source);

                source = source.AsSplitQuery();
                return await source.SearchPageAsync(query);
            });
        }
    }
}
