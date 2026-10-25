using System.Text.Json;

namespace NileCore.Raar.Core.Json
{
    /// <summary>
    /// Extension methods for JSON serialization, deserialization, and deep cloning.
    /// </summary>
    public static class JsonExtensions
    {
        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        public static string ToJson<T>(this T? value, JsonSerializerOptions? options = null) =>
            value is null ? string.Empty : JsonSerializer.Serialize(value, options);

        /// <summary>
        /// Deserializes a JSON string into the specified type.
        /// </summary>
        public static T? FromJson<T>(this string? json, JsonSerializerOptions? options = null) =>
            string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json!, options);

        /// <summary>
        /// Creates a deep clone of an object via JSON serialization.
        /// </summary>
        public static T? DeepClone<T>(this T? value, JsonSerializerOptions? options = null)
        {
            if (value is null) return default;
            var json = JsonSerializer.Serialize(value, options);
            return JsonSerializer.Deserialize<T>(json, options);
        }
    }
}
