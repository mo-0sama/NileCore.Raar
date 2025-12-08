using System;
using ZstdSharp;
namespace NileCore.Raar.Core.Compression
{
    public class ZstdCompressor : ICompressor
    {
        public byte[] Compress(byte[] data)
        {
            using var compressor = new Compressor();
            Span<byte> compressedSpan = compressor.Wrap(data);

            byte[] compressedBytes = compressedSpan.ToArray();

            byte[] result = new byte[8 + compressedBytes.Length];
            BitConverter.GetBytes(data.Length).CopyTo(result, 0);
            BitConverter.GetBytes(compressedBytes.Length).CopyTo(result, 4);
            Buffer.BlockCopy(compressedBytes, 0, result, 8, compressedBytes.Length);

            return result;
        }

        public byte[] Decompress(byte[] data)
        {
            int originalLength = BitConverter.ToInt32(data, 0);
            int compressedLength = BitConverter.ToInt32(data, 4);

            if (compressedLength != data.Length - 8)
                throw new InvalidOperationException("Invalid Zstd payload length");

            byte[] compressedBytes = new byte[compressedLength];
            Buffer.BlockCopy(data, 8, compressedBytes, 0, compressedLength);

            using var decompressor = new Decompressor();
            Span<byte> decompressedSpan = decompressor.Unwrap(compressedBytes);

            byte[] result = decompressedSpan.ToArray();

            if (result.Length != originalLength)
                throw new InvalidOperationException("Zstd decompression size mismatch");

            return result;
        }
    }
}