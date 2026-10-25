using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace NileCore.Raar.Core.Extensions
{
    /// <summary>
    /// Extension methods for enum types and values.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Retrieves the description string from a <see cref="DescriptionAttribute"/> if present; otherwise returns the enum's string value.
        /// </summary>
        public static string GetDescription(this Enum value)
        {
            if (value == null) return string.Empty;

            var field = value.GetType().GetField(value.ToString());
            if (field != null)
            {
                var attr = field.GetCustomAttribute<DescriptionAttribute>();
                if (attr != null && !string.IsNullOrEmpty(attr.Description))
                {
                    return attr.Description;
                }
            }

            return value.ToString();
        }

        /// <summary>
        /// Returns all values of an enum type as a list.
        /// </summary>
        public static List<TEnum> ToList<TEnum>() where TEnum : struct, Enum
        {
            return Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();
        }

        /// <summary>
        /// Safely parses a string into an enum value, returning the fallback default if parsing fails.
        /// </summary>
        public static TEnum Parse<TEnum>(string? value, bool ignoreCase = true, TEnum defaultValue = default) where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            if (Enum.TryParse<TEnum>(value, ignoreCase, out var result))
            {
                return result;
            }
            return defaultValue;
        }
    }
}
