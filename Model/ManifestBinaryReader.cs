using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FrostbiteManifestSystemTools.Model
{
    internal class ManifestBinaryReader : BinaryReader
    {
        public ManifestBinaryReader(FileStream fileStream)
            : base(fileStream) { }

        internal IEnumerable<ChunkDescriptor> ReadChunkDescriptors()
        {
            List<ChunkDescriptor> chunkDescriptors = new List<ChunkDescriptor>();

            while(BaseStream.Position < BaseStream.Length)
            {
                long tryPosition = BaseStream.Position;
                try
                {
                    chunkDescriptors.Add(ReadChunkDescriptor());
                }
                catch (Exception e)
                {
                    BaseStream.Seek(tryPosition, SeekOrigin.Begin);
                    break;
                }
            }

            return chunkDescriptors;
        }


        private ChunkDescriptor ReadChunkDescriptor()
        {
            UInt32 manifestSize = this.ReadUInt32BE();
            long baseOffset = this.BaseStream.Position;
            byte[] manifestContent = this.ReadBytes((int)manifestSize);

            ChunkDescriptor descriptor = new ChunkDescriptor();

            using (BinaryReader memoryReader = new BinaryReader(new MemoryStream(manifestContent)))
            {
                descriptor.Magic = memoryReader.ReadBytes(4);
                descriptor.Offset = baseOffset - 4;
                if (!descriptor.Magic.SequenceEqual(new byte[] { 0x9D, 0x79, 0x8E, 0xD5 }))
                {
                    throw new InvalidDataException();
                }
                descriptor.TotalFiles = memoryReader.ReadUInt32BE();
                descriptor.TotalFilesWithName = memoryReader.ReadUInt32BE();
                descriptor.TotalFilesWithId = memoryReader.ReadUInt32BE();
                descriptor.TotalFilesWithMetaEntries = memoryReader.ReadUInt32BE();
                descriptor.OffsetFileNamesBlock = memoryReader.ReadUInt32BE();
                descriptor.OffsetMetaEntriesBlock = memoryReader.ReadUInt32BE();
                descriptor.MetaEntriesSize = memoryReader.ReadUInt32BE();
                
                for (int i = 0; i < descriptor.TotalFiles; i++)
                {
                    ChunkDescriptorPair pair = new ChunkDescriptorPair();
                    pair.Hash = memoryReader.ReadBytes(20);
                    descriptor.Pairs.Add(pair);
                }

                List<byte[]> chunkIds = new List<byte[]>();

                if (descriptor.TotalFilesWithMetaEntries == 0 && descriptor.TotalFilesWithId != 0)
                {
                    for (int i = 0; i < descriptor.TotalFilesWithName; i++)
                    {
                        descriptor.EbxDescriptors.Add(ReadIntermediateDescriptor(baseOffset, memoryReader));
                    }

                    for (int i = 0; i < descriptor.TotalFilesWithId; i++)
                    {
                        descriptor.DataDescriptors.Add(ReadIntermediateDescriptor(baseOffset, memoryReader));
                    }

                    for (int i = 0; i < descriptor.TotalFilesWithId; i++)
                    {
                        var resourceId = memoryReader.ReadUInt32BE();
                    }

                    for (int i = 0; i < descriptor.TotalFilesWithId; i++)
                    {
                        var unknown1 = memoryReader.ReadUInt64BE();
                        var unknown2 = memoryReader.ReadUInt64BE();
                    }

                    for (int i = 0; i < descriptor.TotalFilesWithId; i++)
                    {
                        chunkIds.Add(memoryReader.ReadBytes(8));
                    }
                }

                memoryReader.BaseStream.Seek(descriptor.OffsetFileNamesBlock, SeekOrigin.Begin);

                int nameStartOffset = 0;
                for (int i = 0; i < descriptor.TotalFilesWithName; i++)
                {
                    descriptor.Pairs[i].Name = memoryReader.ReadNullTerminatedString().Replace('/', '\\');

                    var compositeDescriptor = new CompositeResourceDescriptor();
                    compositeDescriptor.Name = descriptor.Pairs[i].Name;
                    compositeDescriptor.Ebx = ConvertIntermediateResourceToFinalResource(descriptor.EbxDescriptors.FirstOrDefault(ebx => ebx.NameOffset == nameStartOffset), compositeDescriptor);
                    compositeDescriptor.Resource = ConvertIntermediateResourceToFinalResource(descriptor.DataDescriptors.FirstOrDefault(ebx => ebx.NameOffset == nameStartOffset), compositeDescriptor);
                    descriptor.ResourceDescriptors.Add(compositeDescriptor);

                    nameStartOffset += compositeDescriptor.Name.Length + 1;
                }

                if (descriptor.TotalFilesWithMetaEntries == 0 && descriptor.TotalFilesWithId != 0)
                {
                    for (int i = 0; i < descriptor.TotalFilesWithId; i++)
                    {
                        descriptor.Pairs[(int)descriptor.TotalFilesWithName + i].Name = BitConverter.ToString(chunkIds[i]).Replace("-", "");
                    }
                }
            }

            return descriptor;
        }

        internal ChunksManifest ReadManifest()
        {
            UInt32 numberOfChunks = this.ReadUInt32();
            UInt32 numberOfPatches = this.ReadUInt32();
            UInt32 numberOfIds = this.ReadUInt32();
            var chunks = new List<Chunk>();
            var patches = new List<Patch>();
            var identifiers = new List<Identifier>();

            for (int i = 0; i < numberOfChunks; i++)
            {
                var chunk = new Chunk();
                chunk.Archive = this.ReadByte();
                chunk.DirectoryId = this.ReadByte();
                chunk.BaseUnknown = this.ReadUInt16();
                chunk.BasePosition = this.ReadUInt32();
                chunk.BaseLength = this.ReadUInt32();
                chunk.BaseZeros = this.ReadUInt32();

                chunks.Add(chunk);
            }

            for (int i = 0; i < numberOfPatches; i++)
            {
                var patch = new Patch();
                patch.PatchIdentifier = this.ReadUInt32();
                patch.PatchStart = this.ReadUInt32();
                patch.PatchLength = this.ReadUInt32();
                patch.PatchZeros = this.ReadUInt32();
                patch.PatchZeros2 = this.ReadUInt32();

                patches.Add(patch);
            }

            for (int i = 0; i < numberOfIds; i++)
            {
                var identifier = new Identifier();
                identifier.Id = this.ReadBytes(16);
                identifier.PatchStart = this.ReadUInt32();

                identifiers.Add(identifier);
            }

            ChunksManifest manifest = new ChunksManifest()
            {
                Chunks = chunks,
                Patches = patches,
                Identifiers = identifiers
            };

            return manifest;
        }

        private IntermediateResourceDescriptor ReadIntermediateDescriptor(long baseOffset, BinaryReader memoryReader)
        {
            var intermediate = new IntermediateResourceDescriptor();

            intermediate.FileOffset = baseOffset + memoryReader.BaseStream.Position;
            intermediate.NameOffset = memoryReader.ReadInt32BE();
            intermediate.Size = memoryReader.ReadInt32BE();

            return intermediate;
        }

        private ResourceDescriptor ConvertIntermediateResourceToFinalResource(IntermediateResourceDescriptor intermediateResourceDescriptor, CompositeResourceDescriptor compositeDescriptor)
        {
            if (intermediateResourceDescriptor == null)
            {
                return null;
            }

            intermediateResourceDescriptor.FinalResourceDescriptor = compositeDescriptor;

            return new ResourceDescriptor()
            {
                Offset = intermediateResourceDescriptor.FileOffset,
                Size = intermediateResourceDescriptor.Size
            };
        }
    }

    public class ChunksManifest
    {
        public List<Chunk> Chunks { get; set; }
        public List<Patch> Patches { get; set; }
        public List<Identifier> Identifiers { get; set; }
    }

    public class Chunk
    {
        public byte Archive { get; set; }
        public byte DirectoryId { get; set; }
        public UInt16 BaseUnknown { get; set; }
        public UInt32 BasePosition { get; set; }
        public UInt32 BaseLength { get; set; }
        public UInt32 BaseZeros { get; set; }
    }

    public class Patch
    {
        public UInt32 PatchIdentifier { get; set; }
        public UInt32 PatchStart { get; set; }
        public UInt32 PatchLength { get; set; }
        public UInt32 PatchZeros { get; set; }
        public UInt32 PatchZeros2 { get; set; }
    }

    public class Identifier
    {
        public byte[] Id { get; set; }
        public UInt32 PatchStart { get; set; }
    }
}