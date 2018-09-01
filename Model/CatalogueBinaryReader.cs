using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System;

namespace FrostbiteManifestSystemTools.Model
{
    internal class CatalogueBinaryReader : BinaryReader
    {
        public CatalogueBinaryReader(FileStream fileStream)
            : base(fileStream) { }

        public override string ReadString()
        {
            int length = Read7BitEncodedInt();
            return new string(ReadChars(length)).TrimEnd('\0');
        }

        public string ReadNullTerminatedString()
        {
            List<byte> stringBytes = new List<byte>();
            int currentByte;

            while ((currentByte = ReadByte()) != 0x00)
            {
                stringBytes.Add((byte)currentByte);
            }

            return Encoding.ASCII.GetString(stringBytes.ToArray());
        }

        public FrostbiteHeader ReadTableOfContentsHeader()
        {
            FrostbiteHeader header = new FrostbiteHeader();

            header.Signature = ReadBytes(3);
            header.Version = ReadByte();

            ReadInt32();

            if (header.Version == 0x01)
            {
                header.XorKey = ReadBytes(256);
            }
            header.HexValue = new string(ReadChars(258));

            if (header.Version != 0x01)
            {
                ReadBytes(256);
            }

            ReadBytes(34);

            return header;
        }

        public Catalogue ReadCatalogue(byte version)
        {
            Dictionary<byte[], CatalogueEntry> dictionary = new Dictionary<byte[], CatalogueEntry>(new StructuralEqualityComparer());
            List<byte[]> hashes = new List<byte[]>();
            List<byte[]> fileHashes = new List<byte[]>();
            Catalogue catalogue = new Catalogue(((FileStream)BaseStream).Name, dictionary, hashes, fileHashes);

            if (version == 0x01)
            {
                catalogue.Header = ReadTableOfContentsHeader();
            }

            catalogue.Signature = new string(ReadChars(16));

            if (version == 0x01)
            {
                catalogue.NumberOfFiles = ReadInt32();
                catalogue.NumberOfHashes = ReadInt32();

                byte[] extraBytes = ReadBytes(16);
                if (extraBytes[15] == 0)
                {
                    catalogue.Extra = extraBytes;
                }
                else
                {
                    BaseStream.Seek(-16, SeekOrigin.Current);
                }

                for (int i = 0; i < catalogue.NumberOfFiles; i++)
                {
                    BuildCatalogueEntry(catalogue, version);
                }

                for (int i = 0; i < catalogue.NumberOfHashes; i++)
                {
                    hashes.Add(ReadBytes(60));
                }
            }
            else
            {
                while (BaseStream.Position < BaseStream.Length)
                {
                    BuildCatalogueEntry(catalogue, version);
                }
            }

            return catalogue;
        }

        private void BuildCatalogueEntry(Catalogue catalogue, byte version)
        {
            byte[] hash = ReadBytes(20);
            CatalogueEntry entry = new CatalogueEntry();
            entry.Offset = ReadInt32();
            entry.Size = ReadInt32();

            if (version == 0x01)
            {
                entry.Extra = ReadInt32();
            }

            entry.Archive = ReadInt32();

            catalogue.Files[hash] = entry;
            catalogue.FileHashes.Add(hash);
            entry.Parent = catalogue;
            entry.ResolvedName = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
        }
    }
}
