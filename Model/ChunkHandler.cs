using System;
using System.IO;

namespace FrostbiteManifestSystemTools.Model
{
    internal static class ChunkHandler
    {
        public static CompressionStrategyZstd CompressionStrategy = new CompressionStrategyZstd();

        public static Int32 Chunk(BinaryWriter writer, string path)
        {
            Int32 totalFileSize = 0;
            UInt32 uncompressedFileSize = (UInt32)new FileInfo(path).Length;

            using (BinaryReader changedFileReader = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                byte[] inBuffer;
                byte[] outBuffer = new byte[UInt16.MaxValue * 2];

                UInt32 maximalUncompressedChunkSize = UInt16.MaxValue + 1;

                for (UInt32 currentPosition = 0; currentPosition < uncompressedFileSize; currentPosition += maximalUncompressedChunkSize)
                {
                    UInt32 uncompressedChunkSize = uncompressedFileSize - currentPosition;
                    if (uncompressedChunkSize > maximalUncompressedChunkSize)
                    {
                        uncompressedChunkSize = maximalUncompressedChunkSize;
                    }

                    inBuffer = changedFileReader.ReadBytes((int)uncompressedChunkSize);

                    int compressedChunkSize = CompressionStrategy.Compress(inBuffer, outBuffer, (int)uncompressedChunkSize);

                    writer.WriteUInt32BE(uncompressedChunkSize);
                    totalFileSize += 8;

                    // store uncompressed
                    if (compressedChunkSize > uncompressedChunkSize)
                    {
                        writer.Write((byte)0);
                        writer.Write((byte)0x71);
                        writer.Write((UInt16)0);
                        writer.Write(inBuffer);
                        totalFileSize += (int)uncompressedChunkSize;
                    }
                    // store compressed
                    else
                    {
                        writer.Write(CompressionStrategy.CompressionSignature);
                        writer.Write((byte)0x70);
                        writer.WriteUInt16BE((UInt16)compressedChunkSize);
                        writer.Write(outBuffer, 0, compressedChunkSize);
                        totalFileSize += compressedChunkSize;
                    }
                }
            }

            return totalFileSize;
        }

        public static int Dechunk(string finalName, BinaryReader reader, int size)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(finalName, FileMode.Create)))
            {
                return Dechunk(writer, reader, size);
            }
        }

        public static int Dechunk(BinaryWriter writer, BinaryReader reader, int size)
        {
            int uncompressedFileSize = 0;

            using (BinaryReader chunkedFileReader = new BinaryReader(new MemoryStream(reader.ReadBytes(size))))
            {
                while (chunkedFileReader.BaseStream.Position < size)
                {
                    UInt32 uncompressedSize = chunkedFileReader.ReadUInt32BE();
                    byte compressionType = chunkedFileReader.ReadByte();
                    byte compressionSignature = chunkedFileReader.ReadByte();
                    UInt16 compressedSize = chunkedFileReader.ReadUInt16BE();

                    if (compressionType == 0x00)
                    {
                        writer.Write(chunkedFileReader.ReadBytes((int)uncompressedSize));
                        uncompressedFileSize += (int)uncompressedSize;
                    }
                    else if (compressionType == 0x0F)
                    {
                        ZstdNet.Decompressor decompressor = new ZstdNet.Decompressor();
                        byte[] input = chunkedFileReader.ReadBytes(compressedSize);
                        byte[] output = decompressor.Unwrap(input);
                        writer.Write(output);
                        uncompressedFileSize += output.Length;
                    }
                }
            }

            return uncompressedFileSize;
        }
    }
}
