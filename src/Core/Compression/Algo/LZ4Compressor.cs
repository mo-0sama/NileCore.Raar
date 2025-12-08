using K4os.Compression.LZ4;
using System;
namespace NileCore.Raar.Core.Compression
{
    public class Lz4Compressor : ICompressor
    {
        public byte[] Compress(byte[] data)
        {
            int maxSize = LZ4Codec.MaximumOutputSize(data.Length);
            byte[] compressed = new byte[maxSize];

            int compressedLength = LZ4Codec.Encode(
                data, 0, data.Length,
                compressed, 0, compressed.Length
            );

            Array.Resize(ref compressed, compressedLength);

            byte[] result = new byte[4 + 4 + compressedLength];

            BitConverter.GetBytes(data.Length).CopyTo(result, 0);
            BitConverter.GetBytes(compressedLength).CopyTo(result, 4);
            Buffer.BlockCopy(compressed, 0, result, 8, compressedLength);

            return result;
        }

        public byte[] Decompress(byte[] data)
        {
            int originalLength = BitConverter.ToInt32(data, 0);
            int compressedLength = BitConverter.ToInt32(data, 4);

            if (compressedLength != (data.Length - 8))
                throw new InvalidOperationException("Invalid LZ4 payload length");

            byte[] result = new byte[originalLength];

            int decoded = LZ4Codec.Decode(
                data, 8, compressedLength,
                result, 0, originalLength
            );

            if (decoded != originalLength)
                throw new InvalidOperationException("LZ4 decompression failed");

            return result;
        }
    }
}