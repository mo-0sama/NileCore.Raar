using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NileCore.Raar.Core.Extensions
{
    public static class ObjectExtenstions
    {
        public static Dictionary<string, object> ToDictionary(this object obj) =>
          obj.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(obj));
        public static bool IsEmpty<T>(this T value)
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

            // ICollection<T>
            if (value is System.Collections.ICollection c)
                return c.Count == 0;

            // IEnumerable<T>
            if (value is System.Collections.IEnumerable e)
                return !e.GetEnumerator().MoveNext();

            // Value types (int, double, bool, enums)
            if (typeof(T).IsValueType)
                return value.Equals(default(T));

            return false;
        }

    }
}