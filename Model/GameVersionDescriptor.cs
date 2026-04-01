namespace FrostbiteManifestSystemTools.Model
{
    public class GameVersionDescriptor
    {
        public string EnglishChunkGuid { get; set; }
        public int LayoutTocManifestSha1Location { get; set; }
        public int ManifestCatSha1Location { get; set; }
        public string ManifestChunk { get; set; }
        public string FontDescriptor { get; set; }
    }
}
