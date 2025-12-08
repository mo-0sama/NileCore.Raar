using NileCore.Raar.Abstractions.Compression;
using System.IO;
using System.IO.Compression;

namespace NileCore.Raar.Core.Compression
{
    public class BrotliCompressor : ICompressor
    {
        public byte[] Compress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var brotli = new BrotliStream(output, CompressionLevel.Optimal))
                brotli.Write(data, 0, data.Length);

            return output.ToArray();
        }

        public byte[] Decompress(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var brotli = new BrotliStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();

            brotli.CopyTo(output);

            return output.ToArray();
        }
    }
}