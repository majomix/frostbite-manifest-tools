using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FrostbiteManifestSystemTools.Model
{
    public class CatalogueBinaryWriter : BinaryWriter
    {
        public CatalogueBinaryWriter(Stream stream)
            : base(stream) { }

        public void WriteNullTerminatedStringWithLEB128Prefix(string value)
        {
            Write7BitEncodedInt(value.Length + 1);
            WriteNullTerminatedString(value);
        }

        public void WriteNullTerminatedString(string value)
        {
            value = value + '\0';
            Write(Encoding.ASCII.GetBytes(value));
        }

        public void Write(FrostbiteHeader header)
        {
            if (header == null) return;

            Write(header.Signature);
            Write(header.Version);
            Write((Int32)0);

            if (header.Version == 0x01)
            {
                Write(header.XorKey);
            }
            Write(Encoding.ASCII.GetBytes(header.HexValue));

            if (header.Version != 0x01)
            {
                Write(new byte[256]);
            }
            Write(new byte[34]);
        }

        public void Write(byte version, Catalogue catalogue)
        {
            if (version == 0x01)
            {
                Write(catalogue.Header);
            }

            Write(Encoding.ASCII.GetBytes(catalogue.Signature));

            if (version == 0x01)
            {
                Write(catalogue.NumberOfFiles);
                Write(catalogue.NumberOfHashes);
                if (catalogue.Extra != null)
                {
                    Write(catalogue.Extra);
                }
            }

            foreach (var fileHash in catalogue.FileHashes)
            {
                Write(version, fileHash, catalogue.Files[fileHash]);
            }

            foreach (byte[] hash in catalogue.Hashes)
            {
                Write(hash);
            }
        }

        public void Write(byte version, byte[] key, CatalogueEntry entry)
        {
            Write(key);
            Write(entry.Offset);
            Write(entry.Size);
            if (version == 0x01)
            {
                Write(entry.Extra);
            }
            Write(entry.Archive);
        }
    }
}
