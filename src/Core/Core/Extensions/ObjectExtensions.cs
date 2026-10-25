using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NileCore.Raar.Core.Extensions
{
    /// <summary>
    /// Extension methods for object reflection and emptiness checks.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Converts the public properties of an object into a Dictionary.
        /// </summary>
        public static Dictionary<string, object?> ToDictionary(this object obj)
        {
            if (obj == null) return new Dictionary<string, object?>();
            return obj.GetType().GetProperties().ToDictionary(p => p.Name, p => (object?)p.GetValue(obj));
        }

        /// <summary>
        /// Determines whether a value is null, empty, whitespace, or default.
        /// Properly disposes any enumerators created during evaluation.
        /// </summary>
        public static bool IsEmpty<T>(this T? value)
        {
            if (value == null) return true;

            // String
            if (value is string str)
                return string.IsNullOrWhiteSpace(str);

            // Guid
            if (value is Guid guid)
                return guid == Guid.Empty;

            // Array
            if (value is Array arr)
                return arr.Length == 0;

            // IList (covers List<T>, ArrayList)
            if (value is IList list)
                return list.Count == 0;

            // ICollection (covers ICollection<T>, HashSet, etc.)
            if (value is ICollection c)
                return c.Count == 0;

            // IEnumerable
            if (value is IEnumerable e)
            {
                var enumerator = e.GetEnumerator();
                try
                {
                    return !enumerator.MoveNext();
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            }

            // Value types (int, double, bool, enums, structs)
            if (typeof(T).IsValueType)
                return value.Equals(default(T));

            return false;
        }

        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if the object is null.
        /// </summary>
        public static T ThrowIfNull<T>(this T? argument, string? paramName = null) where T : class
        {
            if (argument is null)
                throw new ArgumentNullException(paramName);
            return argument;
        }

        /// <summary>
        /// Transforms an object using the specified mapping delegate.
        /// </summary>
        public static TResult Map<TSource, TResult>(this TSource source, Func<TSource, TResult> mapFunc)
        {
            if (mapFunc == null) throw new ArgumentNullException(nameof(mapFunc));
            return mapFunc(source);
        }

        /// <summary>
        /// Returns an inspection string representation of the object's properties.
        /// </summary>
        public static string Dump<T>(this T? obj)
        {
            if (obj == null) return "null";
            var dict = ToDictionary(obj);
            if (dict.Count == 0) return obj.ToString() ?? string.Empty;
            var sb = new System.Text.StringBuilder();
            sb.Append(typeof(T).Name).Append(" { ");
            bool first = true;
            foreach (var kvp in dict)
            {
                if (!first) sb.Append(", ");
                sb.Append(kvp.Key).Append(": ").Append(kvp.Value?.ToString() ?? "null");
                first = false;
            }
            sb.Append(" }");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Backward-compatible alias for the legacy typo in class name.
    /// </summary>
    [Obsolete("ObjectExtenstions has a typo in its name. Use ObjectExtensions instead.", false)]
    public static class ObjectExtenstions
    {
        [Obsolete("Use ObjectExtensions.ToDictionary instead.", false)]
        public static Dictionary<string, object?> ToDictionary(object obj) =>
            ObjectExtensions.ToDictionary(obj);

        [Obsolete("Use ObjectExtensions.IsEmpty instead.", false)]
        public static bool IsEmpty<T>(T? value) =>
            ObjectExtensions.IsEmpty(value);
    }
}
