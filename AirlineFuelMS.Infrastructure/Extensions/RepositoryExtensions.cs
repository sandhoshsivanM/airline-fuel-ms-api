using System.Linq.Expressions;
using System.Reflection;
using AirlineFuelMS.Core.Attributes;

namespace AirlineFuelMS.Infrastructure.Extensions;

/// <summary>
/// Generic IQueryable filter helper.
/// Mirrors the FM Com.FacilityManagement.FM.Helper.RepositoryExtensions pattern.
///
/// Usage:
///   query.ApplyFilter(searchKeywords: "hindustan", filterKeys: new() { ["CountryId"] = 1, ["IsActive"] = 1 });
///
/// Search: matches any property marked with [Search] (string, int, int?) — multi-word AND on each property.
/// FilterKeys: equality match by property name → int value, supporting int / int? / short / long / byte / bool / enum.
/// includeProperties: optional navigation properties to also search/filter through.
/// </summary>
public static class RepositoryExtensions
{
    public static IQueryable<TEntity> ApplyFilter<TEntity>(
        this IQueryable<TEntity> query,
        string? searchKeywords,
        Dictionary<string, int>? filterKeys = null,
        params Expression<Func<TEntity, object>>[] includeProperties) where TEntity : class
    {
        searchKeywords = searchKeywords?.Trim();
        if (string.IsNullOrEmpty(searchKeywords) && (filterKeys == null || filterKeys.Count == 0))
            return query;

        var parameter = Expression.Parameter(typeof(TEntity), "x");
        Expression? combinedPredicate = null;

        if (!string.IsNullOrEmpty(searchKeywords))
        {
            var searchTerms = searchKeywords.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            combinedPredicate = BuildSearchPredicate<TEntity>(parameter, searchTerms, includeProperties);
        }

        if (filterKeys != null && filterKeys.Count > 0)
        {
            var filterPredicate = BuildFilterPredicate<TEntity>(parameter, filterKeys, includeProperties);
            combinedPredicate = combinedPredicate == null
                ? filterPredicate
                : Expression.AndAlso(combinedPredicate, filterPredicate);
        }

        return combinedPredicate != null
            ? query.Where(Expression.Lambda<Func<TEntity, bool>>(combinedPredicate, parameter))
            : query;
    }

    // ---------- Search predicate ----------

    private static Expression? BuildSearchPredicate<TEntity>(
        ParameterExpression parameter,
        string[] searchTerms,
        Expression<Func<TEntity, object>>[] includeProperties)
    {
        var mainPredicate = BuildTypePredicate(parameter, typeof(TEntity), searchTerms);

        if (includeProperties.Length == 0)
            return mainPredicate;

        return includeProperties
            .Select(prop => ApplySubModelSearch(parameter, prop, searchTerms))
            .Aggregate(mainPredicate, (current, sub) =>
                sub == null ? current : current == null ? sub : Expression.OrElse(current, sub));
    }

    private static Expression? ApplySubModelSearch<TEntity>(
        ParameterExpression parameter,
        Expression<Func<TEntity, object>> includeProperty,
        string[] searchTerms)
    {
        var (subModelType, subPropertyExpression) = GetSubModelTypeAndProperty(includeProperty);
        var subModelAccess = GetPropertyMemberExpression(parameter, includeProperty, out var isSelectExpression);

        if (subModelAccess == null) return null;

        Type elementType = subModelType;
        bool isCollection = IsCollectionType(subModelAccess.Type) || isSelectExpression;
        if (isCollection && !isSelectExpression)
            elementType = GetCollectionElementType(subModelType) ?? subModelType;

        var subParameter = Expression.Parameter(elementType, "sub");
        Expression? propertyExpressions;

        if (isSelectExpression && subPropertyExpression != null)
        {
            var propInfo = (subPropertyExpression.Body as MemberExpression)?.Member as PropertyInfo;
            if (propInfo == null) return null;
            propertyExpressions = GetMatchExpression(subParameter, propInfo, searchTerms).Body;
        }
        else
        {
            propertyExpressions = GetSearchableProperties(elementType)
                .Select(property => GetMatchExpression(subParameter, property, searchTerms).Body)
                .DefaultIfEmpty()
                .Aggregate<Expression?, Expression?>(null, (current, expression) =>
                    current == null ? expression : (expression == null ? current : Expression.OrElse(current, expression)));
        }

        if (propertyExpressions == null) return null;

        var subModelPredicate = Expression.Lambda(propertyExpressions, subParameter);

        if (isCollection)
        {
            var anyMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                .MakeGenericMethod(elementType);
            return Expression.Call(null, anyMethod, subModelAccess, subModelPredicate);
        }
        return Expression.Invoke(subModelPredicate, subModelAccess);
    }

    private static Expression? BuildTypePredicate(
        ParameterExpression parameter,
        Type type,
        string[] searchTerms)
    {
        var searchableProperties = GetSearchableProperties(type).ToList();
        if (searchableProperties.Count == 0) return null;

        return searchableProperties
            .Select(prop => CreateMatchExpression(parameter, prop, searchTerms))
            .Aggregate((Expression?)null, (current, next) =>
                current == null ? next : Expression.OrElse(current, next));
    }

    // ---------- Filter predicate ----------

    private static Expression? BuildFilterPredicate<TEntity>(
        ParameterExpression parameter,
        Dictionary<string, int> filterKeys,
        Expression<Func<TEntity, object>>[] includeProperties)
    {
        Expression? predicate = null;

        foreach (var filter in filterKeys)
        {
            var prop = typeof(TEntity).GetProperty(filter.Key);
            if (prop != null)
                predicate = CombineAnd(predicate, CreateFilterExpression(parameter, prop, filter.Value));
        }

        foreach (var includeProp in includeProperties)
        {
            var (subModelType, _) = GetSubModelTypeAndProperty(includeProp);
            var subModelAccess = GetPropertyMemberExpression(parameter, includeProp, out var isSelectExpression);
            if (subModelAccess == null) continue;

            bool isCollection = IsCollectionType(subModelAccess.Type) || isSelectExpression;
            Type targetType = isCollection
                ? GetCollectionElementType(subModelType) ?? subModelType
                : subModelType;

            var subParameter = Expression.Parameter(targetType, "sub");

            foreach (var filter in filterKeys.Where(f => typeof(TEntity).GetProperty(f.Key) == null))
            {
                var subModelProperty = targetType.GetProperty(filter.Key);
                if (subModelProperty == null) continue;

                var subFilterExpression = CreateFilterExpression(subParameter, subModelProperty, filter.Value);
                var subLambda = Expression.Lambda(subFilterExpression, subParameter);

                if (isCollection)
                {
                    var anyMethod = typeof(Enumerable).GetMethods()
                        .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                        .MakeGenericMethod(targetType);
                    predicate = CombineAnd(predicate, Expression.Call(null, anyMethod, subModelAccess, subLambda));
                }
                else
                {
                    predicate = CombineAnd(predicate, Expression.Invoke(subLambda, subModelAccess));
                }
            }
        }

        return predicate;
    }

    // ---------- String / int match (used for [Search]) ----------

    private static LambdaExpression GetMatchExpression(
        ParameterExpression parameter,
        PropertyInfo property,
        string[] searchTerms)
    {
        var body = BuildMatchBody(Expression.Property(parameter, property), property.PropertyType, searchTerms);
        return Expression.Lambda(body, parameter);
    }

    private static Expression CreateMatchExpression(
        ParameterExpression parameter,
        PropertyInfo property,
        string[] searchTerms)
    {
        return BuildMatchBody(Expression.Property(parameter, property), property.PropertyType, searchTerms);
    }

    private static Expression BuildMatchBody(MemberExpression propertyAccess, Type propertyType, string[] searchTerms)
    {
        var underlying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (underlying == typeof(string))
        {
            var notNull = Expression.NotEqual(propertyAccess, Expression.Constant(null, propertyType));
            var match = MultiTermContains(propertyAccess, searchTerms);
            return Expression.AndAlso(notNull, match);
        }

        if (underlying == typeof(int))
        {
            var toString = typeof(object).GetMethod("ToString")!;
            var asString = Expression.Call(propertyAccess, toString);
            var notNull = Expression.NotEqual(propertyAccess, Expression.Constant(null, propertyType));
            var match = MultiTermContains(asString, searchTerms);
            return Expression.AndAlso(notNull, match);
        }

        throw new ArgumentException($"Unsupported [Search] property type: {underlying}");
    }

    private static Expression MultiTermContains(Expression target, string[] terms)
    {
        var contains = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
        return terms
            .Select(term => (Expression)Expression.Call(target, contains, Expression.Constant(term, typeof(string))))
            .Aggregate((current, next) => Expression.AndAlso(current, next));
    }

    // ---------- Int filter ----------

    private static Expression CreateFilterExpression(
        ParameterExpression parameter,
        PropertyInfo property,
        int value)
    {
        return CreateFilterExpression((Expression)parameter, property, value);
    }

    private static Expression CreateFilterExpression(
        Expression parameterOrSub,
        PropertyInfo property,
        int value)
    {
        var propertyAccess = Expression.Property(parameterOrSub, property);
        var propertyType = property.PropertyType;
        var underlying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        bool isNullable = Nullable.GetUnderlyingType(propertyType) != null;

        if (underlying.IsEnum)
        {
            var enumValue = Enum.ToObject(underlying, value);
            return Expression.Equal(propertyAccess, Expression.Constant(enumValue, propertyType));
        }

        object converted;
        if (underlying == typeof(byte))      converted = Convert.ToByte(value);
        else if (underlying == typeof(bool)) converted = value == 1;
        else if (underlying == typeof(int))  converted = isNullable ? (int?)value : value;
        else if (underlying == typeof(short)) converted = Convert.ToInt16(value);
        else if (underlying == typeof(long))  converted = Convert.ToInt64(value);
        else throw new ArgumentException($"Unsupported filter property type: {underlying}");

        return Expression.Equal(propertyAccess, Expression.Constant(converted, propertyType));
    }

    // ---------- Helpers ----------

    private static Expression? CombineAnd(Expression? current, Expression? next) =>
        current == null ? next : (next == null ? current : Expression.AndAlso(current, next));

    private static IEnumerable<PropertyInfo> GetSearchableProperties(Type type) =>
        type.GetProperties().Where(p => p.GetCustomAttributes(typeof(SearchAttribute), false).Length > 0);

    private static bool IsCollectionType(Type type) =>
        (type.IsGenericType && new[] { typeof(IEnumerable<>), typeof(ICollection<>), typeof(IList<>), typeof(List<>) }
            .Contains(type.GetGenericTypeDefinition()))
        || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

    private static Type? GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsGenericType)
        {
            var def = collectionType.GetGenericTypeDefinition();
            if (def == typeof(IEnumerable<>) || def == typeof(ICollection<>) ||
                def == typeof(IList<>)        || def == typeof(List<>))
                return collectionType.GetGenericArguments()[0];
        }
        return collectionType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    private static (Type subModelType, Expression<Func<object, object>>? subPropertyExpression) GetSubModelTypeAndProperty<TEntity>(
        Expression<Func<TEntity, object>> includeProperty)
    {
        if (includeProperty.Body is UnaryExpression unary)   return (unary.Operand.Type, null);
        if (includeProperty.Body is MemberExpression member) return (member.Type, null);
        if (includeProperty.Body is MethodCallExpression mc && mc.Method.Name == "Select")
        {
            var collectionExpression = mc.Arguments[0] as MemberExpression
                ?? throw new InvalidOperationException("Select must follow a member access.");
            var elementType = GetCollectionElementType(collectionExpression.Type)
                ?? throw new InvalidOperationException("Unable to determine collection element type.");
            var lambda = mc.Arguments[1] as LambdaExpression
                ?? throw new InvalidOperationException("Invalid Select lambda.");
            var p = Expression.Parameter(typeof(object), "p");
            var convertedBody = Expression.Convert(lambda.Body, typeof(object));
            return (elementType, Expression.Lambda<Func<object, object>>(convertedBody, p));
        }
        throw new InvalidOperationException("Invalid include property expression");
    }

    private static MemberExpression? GetPropertyMemberExpression<TEntity>(
        ParameterExpression parameter,
        Expression<Func<TEntity, object>> includeProperty,
        out bool isSelectExpression)
    {
        isSelectExpression = false;
        Expression body = includeProperty.Body;
        if (body is UnaryExpression unary) body = unary.Operand;

        if (body is MemberExpression me)
            return WalkMembers(parameter, me);

        if (body is MethodCallExpression mc && mc.Method.Name == "Select")
        {
            isSelectExpression = true;
            return mc.Arguments[0] is MemberExpression collectionExpression
                ? WalkMembers(parameter, collectionExpression)
                : null;
        }
        return null;
    }

    private static MemberExpression WalkMembers(ParameterExpression parameter, MemberExpression leaf)
    {
        var members = new Stack<MemberExpression>();
        for (var current = leaf; current != null; current = current.Expression as MemberExpression)
            members.Push(current);
        Expression expr = parameter;
        while (members.Count > 0)
            expr = Expression.PropertyOrField(expr, members.Pop().Member.Name);
        return (MemberExpression)expr;
    }
}
