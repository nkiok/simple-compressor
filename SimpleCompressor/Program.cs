using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
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

            //using (var reader = new StreamReader(filepath))
            //{
            //    var bmp = ConvertToBitmap(reader.ReadToEnd(), 100);

            //    bmp.Save(Path.ChangeExtension(filepath, "bmp"), System.Drawing.Imaging.ImageFormat.Bmp);
            //}
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

                var bitsRequired = GetMinimumNumberOfBits(map.Count);

                var chunkSize = 32 / bitsRequired;

                var bytesToRead = reader.Length;

                var buffer = new byte[chunkSize];

                do
                {
                    var bytesRead = reader.Read(buffer);

                    var bitvector = new BitVector32(0);

                    var sections = CreateBitSections(chunkSize, (short)map.Count);

                    for (var i = 0; i < bytesRead; i++)
                    {
                        bitvector[sections[i]] = map.IndexOf(buffer[i]);
                    }

                    writer.Write(BitConverter.GetBytes(bitvector.Data));

                    bytesToRead -= bytesRead;
                } while (bytesToRead > 0);
            }
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

                var bitsRequired = GetMinimumNumberOfBits(map.Length);

                var chunkSize = 32 / bitsRequired;

                do
                {
                    var bitvector = new BitVector32(Get32BitValue(reader));

                    bytesToRead -= 4;

                    var sections = CreateBitSections(chunkSize, (short)map.Length);

                    for (var i = 0; i < chunkSize; i++)
                    {
                        writer.WriteByte(map[bitvector[sections[i]]]);
                    }
                } while (bytesToRead > 0);
            }
        }

        private static FileStream GetInputAsStream(string filepath)
        {
            return File.OpenRead(filepath);
        }

        private static FileStream GetOutputAsStream(string filepath)
        {
            return File.OpenWrite(filepath);
        }

        private static int Get32BitValue(FileStream stream)
        {
            var value = new byte[4];

            stream.Read(value);

            return BitConverter.ToInt32(value);
        }

        private static List<BitVector32.Section> CreateBitSections(int numberOfSections, short maxValue)
        {
            var sections = new List<BitVector32.Section>();

            for (var i = 0; i < numberOfSections; i++)
            {
                sections.Add(i == 0
                    ? BitVector32.CreateSection(maxValue)
                    : BitVector32.CreateSection(maxValue, sections[i - 1]));
            }

            return sections;
        }

        private static int GetMinimumNumberOfBits(int value)
        {
            var r = 1;

            while ((value >>= 1) != 0)
            {
                r++;
            }

            return r;
        }

        private static Bitmap ConvertToBitmap(string s, int width)
        {
            var bmp = new Bitmap(width, width);

            for (var i = 0; i < s.Length; i++)
            {
                var y = i / width;

                var x = i - (y * width);

                bmp.SetPixel(x, y, Color.FromArgb(s[i], s[i], s[i]));    
            }

            return bmp;
        }
    }
}
