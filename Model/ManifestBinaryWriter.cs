using System.IO;

namespace FrostbiteManifestSystemTools.Model
{
    internal class ManifestBinaryWriter : BinaryWriter
    {
        public ManifestBinaryWriter(Stream stream)
            : base(stream) { }

        internal void Write(ChunksManifest manifest)
        {
            this.Write(manifest.Chunks.Count);
            this.Write(manifest.Patches.Count);
            this.Write(manifest.Identifiers.Count);

            foreach (var chunk in manifest.Chunks)
            {
                this.Write(chunk.Archive);
                this.Write(chunk.DirectoryId);
                this.Write(chunk.BaseUnknown);
                this.Write(chunk.BasePosition);
                this.Write(chunk.BaseLength);
                this.Write(chunk.BaseZeros);
            }

            foreach (var patch in manifest.Patches)
            {
                this.Write(patch.PatchIdentifier);
                this.Write(patch.PatchStart);
                this.Write(patch.PatchLength);
                this.Write(patch.PatchZeros);
                this.Write(patch.PatchZeros2);
            }

            foreach (var id in manifest.Identifiers)
            {
                this.Write(id.Id);
                this.Write(id.PatchStart);
            }
        }
    }
}