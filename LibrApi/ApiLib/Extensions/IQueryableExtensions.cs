using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace ApiLib.Extensions
{
    public static class IQueryableExtensions
    {

        // Sorting
        public static IQueryable<T> ApplySortOnFields<T>(this IQueryable<T> query, string? ascFields, string? descFields)
        {
            IOrderedQueryable<T>? orderedQuery = null;

            // extraire les champs de tri
            var ascendingFields = ascFields?.Split(',') ?? [];
            var descendingFields = descFields?.Split(',') ?? [];

            // appliquer les tris ascendants
            foreach (var field in ascendingFields)
            {
                orderedQuery = orderedQuery == null
                    ? query.SortBy(field)
                    : orderedQuery.ThenSortBy(field);
            }

            // appliquer les tris descendants
            foreach (var field in descendingFields)
            {
                orderedQuery = orderedQuery == null
                    ? query.SortByDescending(field)
                    : orderedQuery.ThenSortByDescending(field);
            }

            return orderedQuery ?? query;
        }



        public static IOrderedQueryable<T> SortBy<T>(this IQueryable<T> source, string field)
        {
            return source.SortByFunc(field, (query, keySelector) => query.OrderBy(keySelector));
        }

        public static IOrderedQueryable<T> SortByDescending<T>(this IQueryable<T> source, string field)
        {
            return source.SortByFunc(field, (query, keySelector) => query.OrderByDescending(keySelector));
        }

        public static IOrderedQueryable<T> ThenSortBy<T>(this IOrderedQueryable<T> source, string field)
        {
            return source.ThenSortByFunc(field, (query, keySelector) => query.ThenBy(keySelector));
        }

        public static IOrderedQueryable<T> ThenSortByDescending<T>(this IOrderedQueryable<T> source, string field)
        {
            return source.ThenSortByFunc(field, (query, keySelector) => query.ThenByDescending(keySelector));
        }

        public static IOrderedQueryable<T> SortByFunc<T>(this IQueryable<T> source, string field,
                            Func<IQueryable<T>, Expression<Func<T, object>>, IOrderedQueryable<T>> sortingFunction)
        {
            return sortingFunction(source, field.BuildFieldAccessExpression<T>());
        }

        public static IOrderedQueryable<T> ThenSortByFunc<T>(this IOrderedQueryable<T> source, string field,
                            Func<IOrderedQueryable<T>, Expression<Func<T, object>>, IOrderedQueryable<T>> sortingFunction)
        {
            return sortingFunction(source, field.BuildFieldAccessExpression<T>());
        }

        // Filtering
        public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, string key, string value)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = typeof(T).GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                ?? throw new ArgumentException($"Property '{key}' not found on type '{typeof(T).Name}'.");

            var expressions = new List<Expression>();

            // essayer de parser en tant qu'intervalle
            if (value.TryParseRange(property.PropertyType, out var lowerBound, out var upperBound))
            {
                var member = Expression.Property(parameter, property);
                Expression? rangeExpression = null;

                if (lowerBound != null)
                    rangeExpression = Expression.GreaterThanOrEqual(member, Expression.Constant(lowerBound));

                if (upperBound != null)
                {
                    var upperComparison = Expression.LessThanOrEqual(member, Expression.Constant(upperBound));
                    rangeExpression = rangeExpression != null ? Expression.AndAlso(rangeExpression, upperComparison) : upperComparison;
                }

                if (rangeExpression != null)
                    expressions.Add(rangeExpression);
            }
            else
            {
                // si ce n'est pas une plage traiter comme une liste de valeurs séparées par des virgules
                var values = value.Split(',');

                foreach (var val in values)
                {

                    if (property.PropertyType == typeof(DateTime) && DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
                    {
                        var member = Expression.Property(parameter, property);
                        var lowerBoundDate = Expression.Constant(dateValue);
                        var upperBoundDate = Expression.Constant(dateValue.AddDays(1).AddTicks(-1)); // Fin de la journée

                        var greaterThanOrEqual = Expression.GreaterThanOrEqual(member, lowerBoundDate);
                        var lessThanOrEqual = Expression.LessThanOrEqual(member, upperBoundDate);
                        var dateRangeExpression = Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);

                        expressions.Add(dateRangeExpression);
                    }
                    else if (val.TryParseValue(property.PropertyType, out var parsedValue))
                    {
                        var member = Expression.Property(parameter, property);
                        var constant = Expression.Constant(parsedValue);
                        expressions.Add(Expression.Equal(member, constant));
                    }
                }
            }

            // appliquer le filtre si des expressions valides ont été créées
            if (expressions.Any())
            {
                var orExpression = expressions.Aggregate(Expression.OrElse);
                var lambda = Expression.Lambda<Func<T, bool>>(orExpression, parameter);
                query = query.Where(lambda);
            }

            return query;
        }

        public static IQueryable<T> ApplySearchFilter<T>(this IQueryable<T> query, string key, string value)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = typeof(T).GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                ?? throw new ArgumentException($"Property '{key}' not found on type '{typeof(T).Name}'.");

            var expressions = new List<Expression>();

            // Handle wildcard searches (*napoli*)
            if (value.Contains("*"))
            {
                var searchValue = value.Replace("*", "%"); // Convert * to SQL-style wildcard
                var member = Expression.Property(parameter, property);
                var method = typeof(string).GetMethod("Contains", new[] { typeof(string) });

                if (method != null)
                {
                    var constant = Expression.Constant(searchValue.Trim('%'));
                    expressions.Add(Expression.Call(member, method, constant));
                }
            }
            else
            {
                // Handle multiple values (e.g., type=pizza,pasta)
                var values = value.Split(',');

                foreach (var val in values)
                {
                    if (val.TryParseValue(property.PropertyType, out var parsedValue))
                    {
                        var member = Expression.Property(parameter, property);
                        var constant = Expression.Constant(parsedValue);
                        expressions.Add(Expression.Equal(member, constant));
                    }
                }
            }

            if (expressions.Any())
            {
                var orExpression = expressions.Aggregate(Expression.OrElse);
                var lambda = Expression.Lambda<Func<T, bool>>(orExpression, parameter);
                query = query.Where(lambda);
            }

            return query;
        }


    }
}
