using System;
using System.Security.Cryptography;
using System.Text;

namespace NileCore.Raar.Abstractions.Extensions
{
    public static class StringExtensions
    {
        #region Hash 
        public static string HashSHA256(this string input)
        {
            using var sha256Hash = SHA256.Create();
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder builder = new StringBuilder();
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
        #endregion

        #region Format
        public static string AddLeadingZero(this string val, int length)
        {
            while (val.Length < length) val = "0" + val;
            return val;
        }
        #endregion
        #region Mask
        public static string Mask(this string source, int start, int maskLength) => source.Mask(start, maskLength, 'X');

        public static string Mask(this string source, int start, int maskLength, char maskCharacter)
        {
            if (start > source.Length - 1)
                throw new ArgumentException("Start position is greater than string length");
            if (maskLength > source.Length)
                throw new ArgumentException("Mask length is greater than string length");
            if (start + maskLength > source.Length)
                throw new ArgumentException("Start position and mask length imply more characters than are present");
            var mask = new string(maskCharacter, maskLength);
            var unMaskStart = source[..start];
            var unMaskEnd = source[(start + maskLength)..];
            return string.Concat(unMaskStart, mask, unMaskEnd);
        }
        #endregion
    }
}