using System;
using System.Text;

namespace NileCore.Raar.Core.Utilities
{
    /// <summary>
    /// Fast non-cryptographic hashing algorithms (xxHash32 and MurmurHash3_x86_32).
    /// </summary>
    public static class FastHashes
    {
        private const uint Prime32_1 = 2654435761U;
        private const uint Prime32_2 = 2246822519U;
        private const uint Prime32_3 = 3266489917U;
        private const uint Prime32_4 = 668265263U;
        private const uint Prime32_5 = 374761393U;

        private static uint RotateLeft(uint value, int count) => (value << count) | (value >> (32 - count));

        /// <summary>
        /// Computes 32-bit xxHash of byte array.
        /// </summary>
        public static uint XxHash32(byte[] data, uint seed = 0)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            int length = data.Length;
            int offset = 0;
            uint h32;

            if (length >= 16)
            {
                int limit = length - 16;
                uint v1 = seed + Prime32_1 + Prime32_2;
                uint v2 = seed + Prime32_2;
                uint v3 = seed + 0;
                uint v4 = seed - Prime32_1;

                do
                {
                    v1 += BitConverter.ToUInt32(data, offset) * Prime32_2;
                    v1 = RotateLeft(v1, 13) * Prime32_1;
                    offset += 4;

                    v2 += BitConverter.ToUInt32(data, offset) * Prime32_2;
                    v2 = RotateLeft(v2, 13) * Prime32_1;
                    offset += 4;

                    v3 += BitConverter.ToUInt32(data, offset) * Prime32_2;
                    v3 = RotateLeft(v3, 13) * Prime32_1;
                    offset += 4;

                    v4 += BitConverter.ToUInt32(data, offset) * Prime32_2;
                    v4 = RotateLeft(v4, 13) * Prime32_1;
                    offset += 4;
                } while (offset <= limit);

                h32 = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
            }
            else
            {
                h32 = seed + Prime32_5;
            }

            h32 += (uint)length;

            while (offset <= length - 4)
            {
                h32 += BitConverter.ToUInt32(data, offset) * Prime32_3;
                h32 = RotateLeft(h32, 17) * Prime32_4;
                offset += 4;
            }

            while (offset < length)
            {
                h32 += data[offset] * Prime32_5;
                h32 = RotateLeft(h32, 11) * Prime32_1;
                offset++;
            }

            h32 ^= h32 >> 15;
            h32 *= Prime32_2;
            h32 ^= h32 >> 13;
            h32 *= Prime32_3;
            h32 ^= h32 >> 16;

            return h32;
        }

        /// <summary>
        /// Computes 32-bit MurmurHash3 of byte array.
        /// </summary>
        public static uint MurmurHash3_32(byte[] data, uint seed = 0)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            const uint c1 = 0xcc9e2d51;
            const uint c2 = 0x1b873593;

            int length = data.Length;
            int nblocks = length / 4;
            uint h1 = seed;

            for (int i = 0; i < nblocks; i++)
            {
                uint k1 = BitConverter.ToUInt32(data, i * 4);

                k1 *= c1;
                k1 = RotateLeft(k1, 15);
                k1 *= c2;

                h1 ^= k1;
                h1 = RotateLeft(h1, 13);
                h1 = h1 * 5 + 0xe6546b64;
            }

            int tailOffset = nblocks * 4;
            uint tailK1 = 0;

            switch (length & 3)
            {
                case 3:
                    tailK1 ^= (uint)data[tailOffset + 2] << 16;
                    goto case 2;
                case 2:
                    tailK1 ^= (uint)data[tailOffset + 1] << 8;
                    goto case 1;
                case 1:
                    tailK1 ^= data[tailOffset];
                    tailK1 *= c1;
                    tailK1 = RotateLeft(tailK1, 15);
                    tailK1 *= c2;
                    h1 ^= tailK1;
                    break;
            }

            h1 ^= (uint)length;
            h1 ^= h1 >> 16;
            h1 *= 0x85ebca6b;
            h1 ^= h1 >> 13;
            h1 *= 0xc2b2ae35;
            h1 ^= h1 >> 16;

            return h1;
        }
    }
}
