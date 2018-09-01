using System;
using System.Collections.Generic;

namespace FrostbiteManifestSystemTools.Model
{
    public class ChunkDescriptor
    {
        public byte[] Magic { get; set; }
        public Int64 Offset { get; set; }
        public UInt32 TotalFiles { get; set; }
        public UInt32 TotalFilesWithName { get; set; }
        public UInt32 TotalFilesWithId { get; set; }
        public UInt32 TotalFilesWithMetaEntries { get; set; }
        public UInt32 OffsetFileNamesBlock { get; set; }
        public UInt32 OffsetMetaEntriesBlock { get; set; }
        public UInt32 MetaEntriesSize { get; set; }
        public List<ChunkDescriptorPair> Pairs { get; private set; }
        public List<IntermediateResourceDescriptor> EbxDescriptors { get; private set; }
        public List<IntermediateResourceDescriptor> DataDescriptors { get; private set; }
        public List<CompositeResourceDescriptor> ResourceDescriptors { get; private set; }

        public ChunkDescriptor()
        {
            Pairs = new List<ChunkDescriptorPair>();
            EbxDescriptors = new List<IntermediateResourceDescriptor>();
            DataDescriptors = new List<IntermediateResourceDescriptor>();
            ResourceDescriptors = new List<CompositeResourceDescriptor>();
        }
    }
}
