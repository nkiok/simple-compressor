using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace SimpleCompressor
{
    class Program
    {
        static void Main(string[] args)
        {
            var filepath = args[0];

            Compress(filepath);

            Decompress(Path.ChangeExtension(filepath, "compressed"));
        }

        private static void Compress(string filepath)
        {
            var unique = new HashSet<byte>();

            using (var reader = GetInputAsStream(filepath))
            {
                var bytesToRead = reader.Length;

                do
                {
                    unique.Add((byte) reader.ReadByte());

                    bytesToRead--;
                } while (bytesToRead > 0);
            }

            var map = unique.ToList();

            using (var reader = GetInputAsStream(filepath))
            using (var writer = GetOutputAsStream(Path.ChangeExtension(filepath, "compressed")))
            {
                writer.WriteByte((byte)map.Count);
                writer.Write(map.ToArray());

                const int chunkSize = 6;

                var bytesToRead = reader.Length;

                var buffer = new byte[chunkSize];

                do
                {
                    var bytesRead = reader.Read(buffer);

                    var bitvector = new BitVector32(0);

                    var sections = CreateBitSections(6);

                    for (var i = 0; i < bytesRead; i++)
                    {
                        bitvector[sections[i]] = map.IndexOf(buffer[i]);
                    }

                    writer.Write(BitConverter.GetBytes(bitvector.Data));

                    bytesToRead -= bytesRead;
                } while (bytesToRead > 0);
            }
        }

        private static List<BitVector32.Section> CreateBitSections(int numberOfSections)
        {
            var sections = new List<BitVector32.Section>();

            for (var i = 0; i < numberOfSections; i++)
            {
                sections.Add(i == 0
                    ? BitVector32.CreateSection(15)
                    : BitVector32.CreateSection(15, sections[i - 1]));
            }

            return sections;
        }

        private static void Decompress(string filepath)
        {
            using (var reader = GetInputAsStream(filepath))
            using (var writer = GetOutputAsStream(Path.ChangeExtension(filepath, "decompressed.txt")))
            {
                var bytesToRead = reader.Length;

                var mapCount = reader.ReadByte();

                var map = new byte[mapCount];

                reader.Read(map);

                bytesToRead -= mapCount + 1;

                do
                {
                    var bitvector = new BitVector32(Get32BitValue(reader));

                    bytesToRead -= 4;

                    var sections = CreateBitSections(6);

                    for (var i = 0; i < 6; i++)
                    {
                        writer.WriteByte(map[bitvector[sections[i]]]);
                    }
                } while (bytesToRead > 0);
            }
        }

        public static FileStream GetInputAsStream(string filepath)
        {
            return File.OpenRead(filepath);
        }

        public static FileStream GetOutputAsStream(string filepath)
        {
            return File.OpenWrite(filepath);
        }

        public static int Get32BitValue(FileStream stream)
        {
            var value = new byte[4];

            stream.Read(value);

            return BitConverter.ToInt32(value);
        }
    }
}
