using System;
using NileCore.Raar.Core.Extensions;
using Xunit;

namespace NileCore.Raar.Core.Tests
{
    public class StringExtensionsTests
    {
        [Fact]
        public void HashSHA256_Computes_Valid_64Char_Hex()
        {
            string hash = "hello world".HashSHA256();
            Assert.Equal(64, hash.Length);
            Assert.Equal("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9", hash);
        }

        [Fact]
        public void HashSHA512_Computes_Valid_128Char_Hex()
        {
            string hash = "hello world".HashSHA512();
            Assert.Equal(128, hash.Length);
            Assert.StartsWith("309ecc489c12d6eb4cc40f50c902f2b4d0ed77ee511a7c7a9bcd3ca86d4cd86f989dd35bc5ff499670da34255b45b0cfd830e81f605dcf7dc5542e93ae9cd76f", hash);
        }

        [Fact]
        public void HashMD5_Computes_Valid_32Char_Hex()
        {
            string hash = "hello world".HashMD5();
            Assert.Equal(32, hash.Length);
            Assert.Equal("5eb63bbbe01eeed093cb22bb8f5acdc3", hash);
        }

        [Fact]
        public void Mask_Masks_Middle_Characters()
        {
            string masked = "1234567890".Mask(3, 4);
            Assert.Equal("123XXXX890", masked);
        }

        [Fact]
        public void AddLeadingZero_Pads_Properly()
        {
            Assert.Equal("00042", "42".AddLeadingZero(5));
            Assert.Equal("12345", "12345".AddLeadingZero(3));
        }

        [Fact]
        public void Truncate_Shortens_With_Suffix()
        {
            string truncated = "Long sentence for truncation test".Truncate(15);
            Assert.Equal(15, truncated.Length);
            Assert.Equal("Long sentenc...", truncated);

            string truncated16 = "Long sentence for truncation test".Truncate(16);
            Assert.Equal("Long sentence...", truncated16);
        }

        [Fact]
        public void ToSlug_Generates_Clean_Url_Slug()
        {
            string slug = "Hello World! This is a NileCore Post: 2026 Edition?".ToSlug();
            Assert.Equal("hello-world-this-is-a-nilecore-post-2026-edition", slug);
        }

        [Fact]
        public void Casing_Conversions_Work_Accurately()
        {
            Assert.Equal("userProfileData", "UserProfileData".ToCamelCase());
            Assert.Equal("user_profile_data", "UserProfileData".ToSnakeCase());
            Assert.Equal("user-profile-data", "UserProfileData".ToKebabCase());
        }

        [Fact]
        public void Base64_Encode_Decode_RoundTrip()
        {
            string original = "Testing Base64 Serialization in NileCore.Raar 1.1.0";
            string base64 = original.ToBase64();
            string restored = base64.FromBase64();
            Assert.Equal(original, restored);
        }

        [Fact]
        public void Validation_Email_And_Url()
        {
            Assert.True("support@nilecore.org".IsValidEmail());
            Assert.False("invalid-email".IsValidEmail());

            Assert.True("https://github.com/mo-0sama/NileCore.Raar".IsValidUrl());
            Assert.False("not-a-url".IsValidUrl());
        }

        [Fact]
        public void ContainsAny_Finds_Substrings()
        {
            string text = "The quick brown fox";
            Assert.True(text.ContainsAny(StringComparison.Ordinal, "cat", "fox", "dog"));
            Assert.False(text.ContainsAny(StringComparison.Ordinal, "cat", "bear", "dog"));
        }

        [Fact]
        public void HashXXHash32_And_HashMurmur3_Are_Deterministic()
        {
            string input = "NileCore.Raar fast non-cryptographic hashing benchmark string";

            // xxHash32
            uint xxInt1 = input.HashXXHash32();
            uint xxInt2 = input.HashXXHash32();
            string xxHex = input.HashXXHash();

            Assert.Equal(xxInt1, xxInt2);
            Assert.Equal(8, xxHex.Length);
            Assert.Equal(xxInt1.ToString("x8"), xxHex);

            // MurmurHash3
            uint mmInt1 = input.HashMurmur3_32();
            uint mmInt2 = input.HashMurmur3_32();
            string mmHex = input.HashMurmur3();

            Assert.Equal(mmInt1, mmInt2);
            Assert.Equal(8, mmHex.Length);
            Assert.Equal(mmInt1.ToString("x8"), mmHex);
        }
    }
}
