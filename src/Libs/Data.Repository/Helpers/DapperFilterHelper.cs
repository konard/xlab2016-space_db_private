using Dapper;
using System;
using System.Collections.Generic;

namespace Data.Repository.Helpers
{
    public static class DapperFilterHelper
    {
        public static void Filter(SqlBuilder builder, string memberAccessor, Dictionary<string, FilterOperandExpression> filter, 
            FilterOperator? filterOperator = null)
        {
            foreach (var key in filter.Keys)
            {
                var item = filter[key];
                Filter(builder, memberAccessor, key, item, filterOperator);
            }
        }

        public static void Filter(SqlBuilder builder, string memberAccessor, string key,
            FilterOperandExpression filterOperandExpression,
            FilterOperator? filterOperator = null)
        {
            if (filterOperandExpression.Operand1 is IFilter value)
            {
                throw new NotImplementedException();
            }
            else
            {
                Filter(builder, memberAccessor, key,
                    filterOperandExpression.Operand1,
                    filterOperandExpression.Operand2,
                    filterOperandExpression.Type, filterOperandExpression.Operator, filterOperator);
            }
        }

        public static void Filter(SqlBuilder builder, string memberAccessor, string key,
            object operand1Value,
            object operand2Value, Type operandType,
            FilterOperator @operator, FilterOperator? combineOperator = null)
        {
            var sql = string.Empty;
            dynamic arg = null;

            switch (@operator)
            {
                case FilterOperator.Equals:
                    sql = $@"{memberAccessor}.""{key}""=@{key}";
                    arg = DynamicHelper.CreateDynamic((_) => _.TryAdd(key, operand1Value));
                    break;
                case FilterOperator.GreaterThan:
                    sql = $@"{memberAccessor}.""{key}"">@{key}";
                    arg = DynamicHelper.CreateDynamic((_) => _.TryAdd(key, operand1Value));
                    break;
                case FilterOperator.GreaterThanOrEqual:
                    sql = $@"{memberAccessor}.""{key}"">=@{key}";
                    arg = DynamicHelper.CreateDynamic((_) => _.TryAdd(key, operand1Value));
                    break;
                case FilterOperator.LessThan:
                    sql = $@"{memberAccessor}.""{key}""<@{key}";
                    arg = DynamicHelper.CreateDynamic((_) => _.TryAdd(key, operand1Value));
                    break;
                case FilterOperator.LessThanOrEqual:
                    sql = $@"{memberAccessor}.""{key}""<=@{key}";
                    arg = DynamicHelper.CreateDynamic((_) => _.TryAdd(key, operand1Value));
                    break;
                case FilterOperator.Like:
                    operand1Value = $"%{operand1Value?.ToString().Replace("%", "")}%";
                    sql = $@"{memberAccessor}.""{key}"" like @{key}";
                    arg = DynamicHelper.CreateDynamic((_) => _.TryAdd(key, operand1Value));
                    break;
                case FilterOperator.StartsWith:
                    throw new NotImplementedException();
                    //result = Expression.Call(property, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), operand1);
                    break;
                case FilterOperator.EndsWith:
                    throw new NotImplementedException();
                    //result = Expression.Call(property, typeof(string).GetMethod("EndsWith", new[] { typeof(string) }), operand1);
                    break;
                case FilterOperator.Between:
                    if (operand2Value == null)
                        return;
                    sql = $@"@{key}1<={memberAccessor}.""{key}"" and {memberAccessor}.""{key}""<=@{key}2";
                    arg = DynamicHelper.CreateDynamic((_) =>
                    {
                        _.TryAdd($"{key}1", operand1Value);
                        _.TryAdd($"{key}2", operand2Value);
                    });
                    break;
            }

            if (!string.IsNullOrEmpty(sql))
            {
                if (combineOperator == FilterOperator.Or)
                {
                    builder.OrWhere(sql, arg);
                }
                else
                {
                    builder.Where(sql, arg);
                }
            }
        }
    }
}
