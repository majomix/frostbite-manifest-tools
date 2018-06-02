using System.IO;

namespace FrostbiteManifestSystemTools.Model
{
    internal class ManifestBinaryWriter : BinaryWriter
    {
        public ManifestBinaryWriter(Stream stream)
            : base(stream) { }
    }
}