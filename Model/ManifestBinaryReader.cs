using System.IO;

namespace FrostbiteManifestSystemTools.Model
{
    internal class ManifestBinaryReader : BinaryReader
    {
        public ManifestBinaryReader(FileStream fileStream)
            : base(fileStream) { }
    }
}