using {ProjectName}.domain.exceptions;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace {ProjectName}.infrastructure.nhibernate.filtering;

/// <summary>
/// Filter expression parser
/// </summary>
public static class FilterExpressionParser
{

    /// <summary>
    ///  Takes a collection of filter operators and constructs a lambda expression that can be used to filter objects
    ///  of type T based on the criteria specified in the filter operators.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="operands"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    static public Expression<Func<T, bool>> ParsePredicate<T>(IEnumerable<FilterOperator> operands)
    {
        var parameterExpression = Expression.Parameter(typeof(T), nameof(T).ToLower());

        List<Expression> allCritera = new List<Expression>();
        foreach (FilterOperator filter in operands)
        {
            string propertyName = filter.FieldName.ToPascalCase();
            Expression propertyExpression = Expression.Property(parameterExpression, propertyName);
            IList<string> filterValues = filter.Values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList();

            if (!filterValues.Any())
                throw new ArgumentException("Error getting valid filter values");

            string firstFilter = filterValues[0];
            Expression? criteria = null;
            switch (filter.RelationalOperatorType)
            {
                case RelationalOperator.Contains:
                    propertyExpression = CallToStringMethod<T>(propertyExpression, propertyName);
                    var constant = Expression.Constant(filterValues.FirstOrDefault());
                    MethodInfo? strContainsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
                    if (strContainsMethod != null)
                    {
                        var expressions = new Expression[] { constant };
                        criteria = Expression.Call(propertyExpression, strContainsMethod, expressions);
                    }
                    break;
                case RelationalOperator.GreaterThan:
                    var constantExpression01 = CreateConstantExpression<T>(propertyName, firstFilter);
                    criteria = Expression.GreaterThan(propertyExpression, constantExpression01);
                    break;
                case RelationalOperator.GreaterThanOrEqual:
                    var constantExpression02 = CreateConstantExpression<T>(propertyName, firstFilter);
                    criteria = Expression.GreaterThanOrEqual(propertyExpression, constantExpression02);
                    break;
                case RelationalOperator.LessThan:
                    var constantExpression03 = CreateConstantExpression<T>(propertyName, firstFilter);
                    criteria = Expression.LessThan(propertyExpression, constantExpression03);
                    break;
                case RelationalOperator.LessThanOrEqual:
                    var constantExpression04 = CreateConstantExpression<T>(propertyName, firstFilter);
                    criteria = Expression.LessThanOrEqual(propertyExpression, constantExpression04);
                    break;
                case RelationalOperator.Between:
                    if (filterValues.Count < 2)
                        throw new InvalidFilterArgumentException($"Between operator requires two values, but only one was provided for property {propertyName}");
                    var secondFilter = filterValues[1];
                    if (string.IsNullOrWhiteSpace(secondFilter))
                        throw new InvalidFilterArgumentException($"Second value for Between operator cannot be null or empty for property {propertyName}");

                    var lowerLimitExpression = CreateConstantExpression<T>(propertyName, firstFilter);
                    var upperLimitExpression = CreateConstantExpression<T>(propertyName, secondFilter);
                    Expression lowerLimitCriteria = Expression.GreaterThanOrEqual(propertyExpression, lowerLimitExpression);
                    Expression upperLimitCriteria = Expression.LessThanOrEqual(propertyExpression, upperLimitExpression);
                    criteria = Expression.AndAlso(lowerLimitCriteria, upperLimitCriteria);
                    break;
                //case RelationalOperator.Equal:
                default:
                    propertyExpression = CallToStringMethod<T>(propertyExpression, propertyName);
                    MethodInfo? arrContainsMethod = filterValues.GetType().GetMethod(nameof(filterValues.Contains), new Type[] { typeof(string) });
                    if (arrContainsMethod != null)
                        criteria = Expression.Call(Expression.Constant(filterValues), arrContainsMethod, propertyExpression);
                    break;
            }
            if (criteria != null)
                allCritera.Add(criteria);
        }

        if (!allCritera.Any())
            return Expression.Lambda<Func<T, bool>>(Expression.Constant(true), parameterExpression);

        Expression? expression = null;
        foreach (Expression criteria in allCritera)
            expression = expression != null ? Expression.AndAlso(expression, criteria) : criteria;
        if (expression == null)
            throw new ArgumentException("No valid criteria found for the filter operands");

        var lambda = Expression.Lambda<Func<T, bool>>(expression, parameterExpression);
        return lambda;
    }

    /// <param name="expression"></param>
    /// <param name="quickSearch"></param>
    /// <returns></returns>
    static public Expression<Func<T, bool>> ParseQueryValuesToExpression<T>(this Expression<Func<T, bool>> expression, QuickSearch quickSearch)
    {
        Expression<Func<T, bool>> expressionJoin = c => true;
        int index = 0;
        foreach (var propertyName in quickSearch.FieldNames)
        {
            Expression? criteria = null;
            var parameterExpression = Expression.Parameter(typeof(T), nameof(T).ToLower());
            Expression propertyExpression = Expression.Property(parameterExpression, propertyName);
            propertyExpression = FilterExpressionParser.CallToStringMethod<T>(propertyExpression, propertyName);
            var constant = Expression.Constant(quickSearch.Value);
            var strContainsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });
            if (strContainsMethod != null)
            {
                criteria = Expression.Call(propertyExpression, strContainsMethod, new Expression[] { constant });
            }
            if (criteria != null)
            {
                var localExpressionJoin = Expression.Lambda<Func<T, bool>>(criteria, parameterExpression);
                expressionJoin = index == 0 ? localExpressionJoin : ConcatExpressionsOrElse<T>(expressionJoin, localExpressionJoin);
            }
            index++;
        }
        expression = ConcatExpressionsAndAlso<T>(expression, expressionJoin);
        return expression;
    }

    private static Expression<Func<T, bool>> ConcatExpressionsAndAlso<T>(this Expression<Func<T, bool>> expr1,
                                                 Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
        return Expression.Lambda<Func<T, bool>>
              (Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
    }

    private static Expression<Func<T, bool>> ConcatExpressionsOrElse<T>(this Expression<Func<T, bool>> expr1,
                                                 Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
        return Expression.Lambda<Func<T, bool>>
              (Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
    }
    private static Expression CallToStringMethod<T>(Expression propertyExpression, string propertyName)
    {
        PropertyInfo? propertyInfo = typeof(T).GetProperty(propertyName)
            ?? throw new ArgumentNullException($"No property found with name [{propertyName}]");
        if (!propertyInfo.PropertyType.Equals(typeof(string)))
            return Expression.Call(propertyExpression, "ToString", Type.EmptyTypes);

        return propertyExpression;
    }

    private static Expression CreateConstantExpression<T>(string propertyName, string constantValue)
    {
        PropertyInfo? propertyInfo = typeof(T).GetProperty(propertyName)
        ?? throw new ArgumentNullException($"No property found with name [{propertyName}]");

        // Get the underlying type if it's nullable
        var actualType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

        if (actualType == typeof(string))
            return Expression.Constant(constantValue, propertyInfo.PropertyType);

        // Special handling for DateTime
        if (actualType == typeof(DateTime))
        {
            // Try parse with invariant culture first
            if (DateTime.TryParseExact(constantValue,
                new[] { "yyyy-MM-dd" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime dateValue))
            {
                dateValue = DateTime.SpecifyKind(dateValue, DateTimeKind.Utc);
                return Expression.Constant(dateValue, propertyInfo.PropertyType);
            }
            throw new InvalidFilterArgumentException(
                $"Invalid date format. Use yyyy-MM-dd. Value provided: {constantValue} for property {propertyName}",
                propertyName);
        }

        object convertedValue = actualType.IsEnum
            ? Enum.Parse(actualType, constantValue)
            : Convert.ChangeType(constantValue, actualType);

        return Expression.Constant(convertedValue, propertyInfo.PropertyType);
    }
}
