using System;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NileCore.Raar.Caching.HybridCache
{
    /// <summary>
    /// Abstraction for serializing and deserializing cache items for distributed cache storage.
    /// </summary>
    public interface ICacheSerializer
    {
        /// <summary>
        /// Serializes a value into a byte array.
        /// </summary>
        byte[] Serialize<T>(T value);

        /// <summary>
        /// Deserializes a byte array back into an instance of <typeparamref name="T"/>.
        /// </summary>
        T? Deserialize<T>(byte[] data);
    }

    /// <summary>
    /// High-performance cache serializer based on System.Text.Json.
    /// </summary>
    public class SystemTextJsonCacheSerializer : ICacheSerializer
    {
        private readonly JsonSerializerOptions _options;

        public SystemTextJsonCacheSerializer(JsonSerializerOptions? options = null)
        {
            _options = options ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public byte[] Serialize<T>(T value)
        {
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value, _options);
        }

        public T? Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0) return default;
            return System.Text.Json.JsonSerializer.Deserialize<T>(data, _options);
        }
    }

    /// <summary>
    /// Backward-compatible cache serializer using Newtonsoft.Json with support for protected setters.
    /// </summary>
    public class NewtonsoftJsonCacheSerializer : ICacheSerializer
    {
        private static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new ProtectedPropertyContractResolver()
        };

        private readonly JsonSerializerSettings _settings;

        public NewtonsoftJsonCacheSerializer(JsonSerializerSettings? settings = null)
        {
            _settings = settings ?? DefaultSettings;
        }

        public byte[] Serialize<T>(T value)
        {
            string json = JsonConvert.SerializeObject(value, _settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public T? Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0) return default;
            string json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }
    }

    internal class ProtectedPropertyContractResolver : DefaultContractResolver
    {
        protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (member is System.Reflection.PropertyInfo propInfo)
            {
                if (!property.Writable)
                {
                    var setter = propInfo.GetSetMethod(nonPublic: true);
                    if (setter != null)
                    {
                        property.Writable = true;
                    }
                }
            }

            return property;
        }
    }
}
