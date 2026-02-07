using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Data.Repository.Helpers
{
    public static class SortHelper
    {
        public static IQueryable<T> Sort<T>(IQueryable<T> source, string key,
            SortOperand operand, bool thenBy = false)
        {
            if (operand.Operator == SortOperator.Unsorted)
                return source;

            var _ = Expression.Parameter(typeof(T), "_");
            var type = typeof(T);
            var propertyInfo = type.GetProperty(key);
            var property = Expression.Property(_, key);
            //var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), propertyType);

            var lambda = Expression.Lambda(property, _);

            var queryableType = typeof(Queryable);
            var orderByMethod = GetQueryableMethod("OrderBy");
            var orderByDescendingMethod = GetQueryableMethod("OrderByDescending");
            var thenByMethod = GetQueryableMethod("ThenBy");
            var thenByDescendingMethod = GetQueryableMethod("ThenByDescending");

            MethodInfo method = null;

            if (thenBy)
            {
                switch (operand.Operator)
                {
                    case SortOperator.Asc:
                        method = thenByMethod;
                        break;
                    case SortOperator.Desc:
                        method = thenByDescendingMethod;
                        break;
                }
            }
            else
            {
                switch (operand.Operator)
                {
                    case SortOperator.Asc:
                        method = orderByMethod;
                        break;
                    case SortOperator.Desc:
                        method = orderByDescendingMethod;
                        break;
                }
            }

            if (method == null)
                throw new InvalidOperationException(operand.Operator.ToString());

            var genericMethod = method.MakeGenericMethod(type, propertyInfo.PropertyType);

            return (IQueryable<T>)genericMethod.Invoke(genericMethod, new object[] { source, lambda }); ;
        }

        private static MethodInfo GetQueryableMethod(string methodName)
        {
            var queryableType = typeof(Queryable);
            return queryableType.GetMethods()
                .Where(m => m.Name == methodName && m.IsGenericMethodDefinition)
                .Where(m =>
                {
                    var parameters = m.GetParameters().ToList();
                    return parameters.Count == 2;
                }).Single();
        }
    }
}
