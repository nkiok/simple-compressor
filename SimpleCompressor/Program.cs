using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;

namespace SimpleCompression
{
    class Program
    {
        static void Main(string[] args)
        {
            var filepath = @"E:\_dev\_git\simple-compressor\README.md";// args[0];

            var unique = new HashSet<byte>();

            using (var reader = GetInputAsStream(filepath))
            {
                var bytesToRead = reader.Length;

                do
                {
                    unique.Add((byte)reader.ReadByte());

                    bytesToRead--;

                } while (bytesToRead > 0);
            }

            using (var reader = GetInputAsStream(filepath))
            {
                const int chunkSize = 6;

                var bytesToRead = reader.Length;

                var chunk = new byte[chunkSize];

                var b32 = new BitVector32();

                var sections = new List<BitVector32.Section>();

                sections.Add(BitVector32.CreateSection(5));
                sections.Add(BitVector32.CreateSection(5, sections[0]));
                sections.Add(BitVector32.CreateSection(5, sections[1]));
                sections.Add(BitVector32.CreateSection(5, sections[2]));
                sections.Add(BitVector32.CreateSection(5, sections[3]));
                sections.Add(BitVector32.CreateSection(5, sections[4]));

                do
                {
                    var bytesRead = reader.Read(chunk);

                    bytesToRead -= bytesRead;

                } while (bytesToRead > 0);
            }
        }

        public static FileStream GetInputAsStream(string filePath)
        {
            return File.OpenRead(filePath);
        }
    }
}
