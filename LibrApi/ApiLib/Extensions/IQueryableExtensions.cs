using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ApiLib.Extensions
{
    public static class IQueryableExtensions
    {
        public static IOrderedQueryable<T> SortBy<T>(this IQueryable<T> source, string field)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, field);
            var propertyToObject = Expression.Convert(property, typeof(Object));

            return source.OrderBy(Expression.Lambda<Func<T, object>>(propertyToObject, parameter));
        }

        public static IOrderedQueryable<T> SortByDescending<T>(this IQueryable<T> source, string field)
        {
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, field);
            var propertyToObject = Expression.Convert(property, typeof(Object));

            return source.OrderByDescending(Expression.Lambda<Func<T, object>>(propertyToObject, parameter));
        }

    }
}
