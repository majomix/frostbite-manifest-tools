using System;
using System.IO;

namespace FrostbiteManifestSystemTools.Model
{
    public static class Extensions
    {
        public static byte[] Reverse(this byte[] b)
        {
            Array.Reverse(b);
            return b;
        }

        public static UInt16 ReadUInt16BE(this BinaryReader binaryReader)
        {
            return BitConverter.ToUInt16(binaryReader.ReadBytesRequired(sizeof(UInt16)).Reverse(), 0);
        }

        public static Int16 ReadInt16BE(this BinaryReader binaryReader)
        {
            return BitConverter.ToInt16(binaryReader.ReadBytesRequired(sizeof(Int16)).Reverse(), 0);
        }

        public static UInt32 ReadUInt32BE(this BinaryReader binaryReader)
        {
            return BitConverter.ToUInt32(binaryReader.ReadBytesRequired(sizeof(UInt32)).Reverse(), 0);
        }

        public static Int32 ReadInt32BE(this BinaryReader binaryReader)
        {
            return BitConverter.ToInt32(binaryReader.ReadBytesRequired(sizeof(Int32)).Reverse(), 0);
        }

        public static UInt64 ReadUInt64BE(this BinaryReader binaryReader)
        {
            return BitConverter.ToUInt64(binaryReader.ReadBytesRequired(sizeof(UInt64)).Reverse(), 0);
        }

        public static Int64 ReadInt64BE(this BinaryReader binaryReader)
        {
            return BitConverter.ToInt64(binaryReader.ReadBytesRequired(sizeof(Int64)).Reverse(), 0);
        }

        public static byte[] ReadBytesRequired(this BinaryReader binaryReader, int byteCount)
        {
            var result = binaryReader.ReadBytes(byteCount);

            if (result.Length != byteCount)
                throw new EndOfStreamException(string.Format("{0} bytes required from stream, but only {1} returned.", byteCount, result.Length));

            return result;
        }

        public static void WriteUInt16BE(this BinaryWriter binaryWriter, UInt16 value)
        {
            binaryWriter.Write(BitConverter.GetBytes(value).Reverse());
        }

        public static void WriteUInt32BE(this BinaryWriter binaryWriter, UInt32 value)
        {
            binaryWriter.Write(BitConverter.GetBytes(value).Reverse());
        }

        public static void WriteUInt64BE(this BinaryWriter binaryWriter, UInt64 value)
        {
            binaryWriter.Write(BitConverter.GetBytes(value).Reverse());
        }
    }
}