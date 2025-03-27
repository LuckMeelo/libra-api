using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApiLib.Extensions
{
    public static class StringExtensions
    {
        public static Expression<Func<T, object>> BuildFieldAccessExpression<T>(this string field)
        {
            var property = typeof(T).GetProperty(field, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ?? throw new ArgumentException($"field {field} not found on type '{typeof(T).Name}'.");

            var parameter = Expression.Parameter(typeof(T));
            var propertyAccess = Expression.Property(parameter, property);
            var propertyToObject = Expression.Convert(propertyAccess, typeof(Object));

            return (Expression.Lambda<Func<T, object>>(propertyToObject, parameter));
        }


        public static bool TryParseValue(this string value, Type targetType, out object? result)
        {
            result = null;
            try
            {
                if (targetType == typeof(DateTime))
                {
                    if (DateTime.TryParseExact(value, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue))
                    {
                        result = dateTimeValue;
                        return true;
                    }

                    return false;
                }
                else
                {
                    result = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool TryParseRange(this string value, Type type, out object? lowerBound, out object? upperBound)
        {
            lowerBound = null;
            upperBound = null;

            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                var parts = value.Trim('[', ']').Split(',');
                if (parts.Length == 2)
                {
                    if (parts[0].TryParseValue(type, out var low))
                        lowerBound = low;
                    if (parts[1].TryParseValue(type, out var high))
                        upperBound = high;

                    return lowerBound != null || upperBound != null;
                }
            }

            return false;
        }
    }
}
