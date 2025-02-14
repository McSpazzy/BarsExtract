﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BARS
{
    public class BARSAudio
    {
        public string Name;

        public BARSHeader Header;
        public AMTA[] AMTA;
        public BWAV[] BWAV;

        public BARSAudio(FileReader reader)
        {
            Header = new BARSHeader(reader);

            AMTA = new AMTA[Header.Count];

            for (var index = 0; index < Header.Count; ++index)
            {
                try
                {
                    AMTA[index] = new AMTA(reader, Header.Offsets[index], Header.SizeCache[Header.Offsets[index]]);
                        
                }
                catch(Exception)
                {
                    Console.WriteLine(reader.BaseStream.Position);
                }
            }

            reader.Seek(Header.BWAVStart, SeekOrigin.Begin);
            BWAV = AMTA.Select(a =>
            {
                reader.Seek(a.DataOffset, SeekOrigin.Begin);
                return new BWAV(a.Label, reader.ReadBytes((int)a.AudioLength));
            }).ToArray();
        }

        public static BARSAudio Read(string filename)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException(filename);
            var name = Path.GetFileNameWithoutExtension(filename);
            return new BARSAudio(new FileReader(new MemoryStream(File.ReadAllBytes(filename)))) { Name = name};
        }

        public void Export(string path, bool fileAsSubPath = false)
        {
            foreach (var bwav in BWAV)
            {
                var savePath = fileAsSubPath ? $"{path}\\{Name}\\" : $"{path}\\";
                File.WriteAllBytes($@"{savePath}{bwav.Name}.bwav", bwav.Data);
            }
        }

        public IEnumerable<string> List => BWAV.Select(b => b.Name);

    }

    public class BWAV
    {
        public BWAV(string name, byte[] data)
        {
            Name = name;
            Data = data;
        }

        public byte[] Data { get; set; }
        public string Name { get; set; }
    }

    public class AMTA
    {
        public ushort ByteOrderMark;
        public ushort Version { get; set; }
        public uint Length { get; set; }
        public uint DataOffset { get; set; }
        public uint AudioLength { get; set; }
        public string Label { get; set; }

        public AMTA(FileReader reader, uint offset, uint length)
        {
            Read(reader);
            DataOffset = offset;
            AudioLength = length;
        }

        public void Read(FileReader reader)
        {
            var basePosition = reader.BaseStream.Position;

            reader.ReadSignature(4, "AMTA");
            ByteOrderMark = reader.ReadUInt16();
            Version = reader.ReadUInt16();
            Length = reader.ReadUInt32();
            reader.Seek(24);
            var offset = reader.ReadUInt32();

            reader.SeekBegin(basePosition + 36 + offset);
            Label = reader.ReadZeroTerminatedString();
            reader.SeekBegin(basePosition + Length);
        }
    }

    public class BARSHeader
    {
        public int Count;
        public uint[] Hashes;
        public Dictionary<uint, uint> SizeCache = new Dictionary<uint, uint>();
        public List<uint> Offsets;

        public uint AMTAStart { get; set; }
        public uint BWAVStart => Offsets[0];

        public BARSHeader(FileReader reader)
        {
            Read(reader);
        }

        public void Read(FileReader reader)
        {
            reader.ReadSignature(4, "BARS");

            var uk1 = (int)reader.ReadUInt32();
            var bom = reader.ReadUInt16();
            var uk2 = (uint)reader.ReadByte();
            var uk3 = (uint)reader.ReadByte();

            Count = reader.ReadInt32();
            Hashes = reader.ReadMultipleUInt32(Count);

            Offsets = new List<uint>();

            for (var index = 0; index < Count; ++index)
            {
                var dataStart = reader.ReadUInt32();
                if (AMTAStart == 0) AMTAStart = dataStart;

                var offset = reader.ReadUInt32();
                if (offset == 0 || offset == uint.MaxValue)
                {
                    continue;
                }

                Offsets.Add(offset);
            }

            var uniqueOffsets = Offsets.Distinct().OrderBy(a => a).ToArray();

            for (var index = 0; index < uniqueOffsets.Length - 1; ++index)
            {
                SizeCache.Add(uniqueOffsets[index], uniqueOffsets[index + 1] - uniqueOffsets[index]);
            }

            SizeCache.Add(uniqueOffsets[^1], (uint)(reader.BaseStream.Length - uniqueOffsets[^1]));

            reader.Seek(AMTAStart, SeekOrigin.Begin);
        }
    }

    public class FileReader : BinaryReader
    {
        public string ReadSignature(int length, string expectedSignature)
        {
            var realSignature = Encoding.ASCII.GetString(ReadBytes(length));

            if (realSignature != expectedSignature)
            {
                throw new Exception($"Invalid signature {realSignature}! Expected {expectedSignature}.");
            }

            return realSignature;
        }

        public void SeekBegin(long offset) { Seek(offset, SeekOrigin.Begin); }
        public long Seek(long offset, SeekOrigin origin) => this.BaseStream.Seek(offset, origin);
        public long Seek(long offset) => this.Seek(offset, SeekOrigin.Current);

        public string ReadZeroTerminatedString(Encoding encoding = null)
        {
            encoding ??= Encoding.ASCII;

            var byteList = new List<byte>();
            switch (encoding.GetByteCount("a"))
            {
                case 1:
                    for (var index = ReadByte(); index != 0; index = ReadByte())
                        byteList.Add(index);
                    break;
                case 2:
                    for (var index = (uint)ReadUInt16(); index != 0U; index = ReadUInt16())
                    {
                        var bytes = BitConverter.GetBytes(index);
                        byteList.Add(bytes[0]);
                        byteList.Add(bytes[1]);
                    }

                    break;
            }

            return encoding.GetString(byteList.ToArray());
        }

        public uint[] ReadMultipleUInt32(int count) => ReadMultiple(count, ReadUInt32);

        private static T[] ReadMultiple<T>(int count, Func<T> readFunc)
        {
            var objArray = new T[count];
            for (var index = 0; index < count; ++index)
            {
                objArray[index] = readFunc();
            }
            return objArray;
        }

        public FileReader(Stream input) : base(input)
        {
        }
    }
}
