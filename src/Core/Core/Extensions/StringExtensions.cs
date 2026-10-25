using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NileCore.Raar.Core.Extensions
{
    /// <summary>
    /// Extension methods for string manipulation, cryptographic hashing, formatting, casing, and validation.
    /// </summary>
    public static class StringExtensions
    {
        #region Hashing
        /// <summary>
        /// Computes the SHA256 hash of the input string and returns it as a lowercase hex string.
        /// </summary>
        public static string HashSHA256(this string input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return ToHexString(bytes);
        }

        /// <summary>
        /// Computes the SHA512 hash of the input string and returns it as a lowercase hex string.
        /// </summary>
        public static string HashSHA512(this string input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            using var sha512 = SHA512.Create();
            byte[] bytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(input));
            return ToHexString(bytes);
        }

        /// <summary>
        /// Computes the MD5 hash of the input string and returns it as a lowercase hex string.
        /// </summary>
        public static string HashMD5(this string input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            using var md5 = MD5.Create();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return ToHexString(bytes);
        }

        /// <summary>
        /// Computes the fast non-cryptographic xxHash32 integer hash.
        /// </summary>
        public static uint HashXXHash32(this string input, uint seed = 0)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            return Utilities.FastHashes.XxHash32(Encoding.UTF8.GetBytes(input), seed);
        }

        /// <summary>
        /// Computes the fast non-cryptographic xxHash32 hash as a lowercase hex string.
        /// </summary>
        public static string HashXXHash(this string input, uint seed = 0) =>
            HashXXHash32(input, seed).ToString("x8");

        /// <summary>
        /// Computes the fast non-cryptographic MurmurHash3 (32-bit) integer hash.
        /// </summary>
        public static uint HashMurmur3_32(this string input, uint seed = 0)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            return Utilities.FastHashes.MurmurHash3_32(Encoding.UTF8.GetBytes(input), seed);
        }

        /// <summary>
        /// Computes the fast non-cryptographic MurmurHash3 (32-bit) hash as a lowercase hex string.
        /// </summary>
        public static string HashMurmur3(this string input, uint seed = 0) =>
            HashMurmur3_32(input, seed).ToString("x8");

        private static string ToHexString(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
        #endregion

        #region Formatting & Masking
        /// <summary>
        /// Pads the beginning of the string with zeros until it reaches the specified total length.
        /// </summary>
        public static string AddLeadingZero(this string? val, int length)
        {
            if (val == null) return string.Empty.PadLeft(length, '0');
            return val.Length >= length ? val : val.PadLeft(length, '0');
        }

        /// <summary>
        /// Masks a substring with 'X' characters.
        /// </summary>
        public static string Mask(this string source, int start, int maskLength) =>
            source.Mask(start, maskLength, 'X');

        /// <summary>
        /// Masks a substring with the specified mask character.
        /// </summary>
        public static string Mask(this string source, int start, int maskLength, char maskCharacter)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (start < 0 || start > source.Length)
                throw new ArgumentException("Start position is outside string bounds.", nameof(start));
            if (maskLength < 0)
                throw new ArgumentException("Mask length cannot be negative.", nameof(maskLength));
            if (start + maskLength > source.Length)
                throw new ArgumentException("Start position and mask length imply more characters than are present in source.");

            if (maskLength == 0) return source;

            var mask = new string(maskCharacter, maskLength);
            var unMaskStart = source.Substring(0, start);
            var unMaskEnd = source.Substring(start + maskLength);
            return string.Concat(unMaskStart, mask, unMaskEnd);
        }

        /// <summary>
        /// Truncates a string to the specified maximum length and appends an optional suffix.
        /// </summary>
        public static string Truncate(this string? source, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(source) || maxLength <= 0) return string.Empty;
            if (source.Length <= maxLength) return source;

            int take = Math.Max(0, maxLength - (suffix?.Length ?? 0));
            return source.Substring(0, take) + (suffix ?? string.Empty);
        }

        /// <summary>
        /// Safe substring operation that will not throw if bounds exceed the string length.
        /// </summary>
        public static string SubstringSafe(this string? source, int start, int length)
        {
            if (string.IsNullOrEmpty(source) || start >= source.Length || length <= 0)
                return string.Empty;

            int actualStart = Math.Max(0, start);
            int actualLength = Math.Min(length, source.Length - actualStart);
            return source.Substring(actualStart, actualLength);
        }
        #endregion

        #region Casing & Slug
        /// <summary>
        /// Converts the input string into a URL-friendly slug.
        /// </summary>
        public static string ToSlug(this string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            // Remove diacritics / accents
            string normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            string clean = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            // Replace non-alphanumeric characters with hyphens
            clean = Regex.Replace(clean, @"[^a-z0-9\s-]", "");
            clean = Regex.Replace(clean, @"\s+", "-").Trim('-');
            clean = Regex.Replace(clean, @"-+", "-");
            return clean;
        }

        /// <summary>
        /// Converts a string to camelCase.
        /// </summary>
        public static string ToCamelCase(this string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            if (input.Length == 1) return input.ToLowerInvariant();
            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        /// <summary>
        /// Converts a PascalCase or camelCase string to snake_case.
        /// </summary>
        public static string ToSnakeCase(this string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, @"(?<!^)(?=[A-Z])", "_").ToLowerInvariant();
        }

        /// <summary>
        /// Converts a PascalCase or camelCase string to kebab-case.
        /// </summary>
        public static string ToKebabCase(this string? input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return Regex.Replace(input, @"(?<!^)(?=[A-Z])", "-").ToLowerInvariant();
        }
        #endregion

        #region Encoding & Base64
        /// <summary>
        /// Encodes a UTF-8 string to a Base64 string.
        /// </summary>
        public static string ToBase64(this string? input)
        {
            if (input == null) return string.Empty;
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Decodes a Base64 string back into a UTF-8 string.
        /// </summary>
        public static string FromBase64(this string? base64)
        {
            if (string.IsNullOrEmpty(base64)) return string.Empty;
            byte[] bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
        #endregion

        #region Search & Validation
        /// <summary>
        /// Checks whether the string contains any of the specified values.
        /// </summary>
        public static bool ContainsAny(this string? source, StringComparison comparisonType = StringComparison.Ordinal, params string[] values)
        {
            if (string.IsNullOrEmpty(source) || values == null || values.Length == 0) return false;
            foreach (var val in values)
            {
                if (!string.IsNullOrEmpty(val) && source.IndexOf(val, comparisonType) >= 0)
                    return true;
            }
            return false;
        }

        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Validates if the string has a standard email address format.
        /// </summary>
        public static bool IsValidEmail(this string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return EmailRegex.IsMatch(email);
        }

        /// <summary>
        /// Validates if the string is a valid absolute URI/URL.
        /// </summary>
        public static bool IsValidUrl(this string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
        #endregion
    }
}