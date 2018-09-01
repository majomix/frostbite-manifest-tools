using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FrostbiteManifestSystemTools.Model
{
    internal class FrostbiteManifestEditor
    {
        private enum DirectoryNames : byte
        {
            InitialExperienceBase = 0x10,
            InitialExperiencePatch = 0x11
        }

        private readonly string BaseDirectory = null;
        private readonly string EnglishChunkGuid = "04eb61dcb468b4f99f4a3401794b2fa5"; // "A7E9B88A80D41B303CF95E477F544997"; // sha1 FEDC89E7F9094F043D40FF029E3A30BA03C75B2C
        private readonly string GermanChunkGuid = "A9DD793397106B9E641373609DD454FB";
        private readonly string[] FontFilesIds = new string[] { "CAA137EA1D2439DB", "62260D3D59055017", "DB62DBEF268BC7C5", "AB6E7745DAB7B953", "19C1F6E906291FCD", "E04D4AF32E12D179", "986728733207C4C9", "CB2813075165D1E5", "14D8FB3E96E5FB2D", "BEF7A206A6858EB5", "38D47D1C571C48FB" };
        private readonly string[] DataDirs = new string[] { @"\Data\Win32\installation\initialexperience", @"\Patch\Win32\installation\initialexperience" };
        private readonly string LayoutTocPath = @"\Data\layout.toc";
        private readonly int LayoutTocManifestSha1Location = 13411;
        private readonly int ManifestCatSha1Location = 1164692;
        private readonly int EnglishFontDescriptorId = 1235132813;

        private ChunksManifest manifest = null;
        private List<ChunkDescriptor> chunkDescriptors = null;
        private List<Catalogue> catalogues = new List<Catalogue>();
        private byte[] mySessionManifestSha1 = null;

        private byte[] SessionManifestSha1
        {
            get
            {
                if (mySessionManifestSha1 == null)
                {
                    mySessionManifestSha1 = GetCurrentManifestSha1();
                }

                return mySessionManifestSha1;
            }
        }


        public FrostbiteManifestEditor(string baseDirectory)
        {
            BaseDirectory = baseDirectory;
        }

        public void LoadFileStructure()
        {
            using (ManifestBinaryReader manifestReader = new ManifestBinaryReader(File.Open(ResolveManifestCasFile(), FileMode.Open)))
            {
                chunkDescriptors = manifestReader.ReadChunkDescriptors().ToList();
                manifest = manifestReader.ReadManifest();
            }

            foreach (string dir in DataDirs)
            {
                using (CatalogueBinaryReader catReader = new CatalogueBinaryReader(File.Open(BaseDirectory + dir + @"\cas.cat", FileMode.Open)))
                {
                    catalogues.Add(catReader.ReadCatalogue((byte)0x01));
                }
            }
        }

        public void ExtractFontFiles(string outputDirectory)
        {
            var fontPatchDescriptor = manifest.Patches.First(patch => patch.PatchIdentifier == EnglishFontDescriptorId);
            var manifestChunkDescriptorIndex = fontPatchDescriptor.PatchStart;
            var manifestChunk = manifest.Chunks[(int)manifestChunkDescriptorIndex];
            var chunkDescriptor = chunkDescriptors.First(desc => desc.Offset == manifestChunk.BasePosition);
            var fileLengthsMapping = new Queue<int>();

            using (StreamWriter descriptorWriter = new StreamWriter(File.Open(outputDirectory + @"\fonts.descriptor", FileMode.Create)))
            {
                using (var completeDecompressedMemoryStream = new MemoryStream())
                {
                    for (int i = 1; i < fontPatchDescriptor.PatchLength; i++)
                    {
                        var patchChunkDescriptor = manifest.Chunks[(int)manifestChunkDescriptorIndex + i];

                        if (patchChunkDescriptor.DirectoryId != (byte)DirectoryNames.InitialExperienceBase && patchChunkDescriptor.DirectoryId != (byte)DirectoryNames.InitialExperiencePatch)
                        {
                            throw new ArgumentException("Not supported");
                        }

                        var dataDir = patchChunkDescriptor.DirectoryId == (byte)DirectoryNames.InitialExperienceBase ? DataDirs[0] : DataDirs[1];

                        string compoundName = BaseDirectory + dataDir + @"\cas_" + (patchChunkDescriptor.Archive + 1).ToString("00") + ".cas";
                        using (BinaryReader reader = new BinaryReader(File.Open(compoundName, FileMode.Open)))
                        {
                            using (BinaryWriter writer = new BinaryWriter(completeDecompressedMemoryStream, ASCIIEncoding.ASCII, true))
                            {
                                reader.BaseStream.Seek(patchChunkDescriptor.BasePosition, SeekOrigin.Begin);
                                var decompressedSize = ChunkHandler.Dechunk(writer, reader, (int)patchChunkDescriptor.BaseLength);
                                fileLengthsMapping.Enqueue(decompressedSize);
                            }
                        }
                    }

                    completeDecompressedMemoryStream.Seek(0, SeekOrigin.Begin);
                    var currentTargetLength = fileLengthsMapping.Dequeue();
                    var subtotalLength = 0;

                    foreach (var ebx in chunkDescriptor.EbxDescriptors)
                    {
                        var resourceName = BuildResourceName("ebx", ebx);
                        ExtractFileFromIntermediateResourceDescriptor(outputDirectory, completeDecompressedMemoryStream, ebx, resourceName);
                        WriteToLengthsDescriptor(fileLengthsMapping, descriptorWriter, ref currentTargetLength, ref subtotalLength, ebx, resourceName);
                    }

                    foreach (var data in chunkDescriptor.DataDescriptors)
                    {
                        var resourceName = BuildResourceName("data", data);
                        ExtractFileFromIntermediateResourceDescriptor(outputDirectory, completeDecompressedMemoryStream, data, BuildResourceName("data", data));
                        WriteToLengthsDescriptor(fileLengthsMapping, descriptorWriter, ref currentTargetLength, ref subtotalLength, data, resourceName);
                    }
                }
            }
        }

        public void ExtractTextFile(string outputDirectory)
        {
            WriteFileFromManifest(LocateLanguageFileChunk(), outputDirectory, EnglishChunkGuid);
        }

        public void WriteFileFromCatalogue(CatalogueEntry entry, string directory)
        {
            string compoundName = Path.GetDirectoryName(entry.Parent.Path) + @"\cas_" + entry.Archive.ToString("00") + ".cas";
            using (BinaryReader reader = new BinaryReader(File.Open(compoundName, FileMode.Open)))
            {
                reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);

                string finalDirectory = directory + @"\cat\";
                string finalName = finalDirectory + entry.ResolvedName;
                Directory.CreateDirectory(finalDirectory);

                ChunkHandler.Dechunk(finalName, reader, entry.Size);
            }
        }

        public void WriteFileFromManifest(Chunk chunk, string directory, string fileName)
        {
            string compoundName = ResolveLanguageCasFile(chunk);

            using (BinaryReader reader = new BinaryReader(File.Open(compoundName, FileMode.Open)))
            {
                reader.BaseStream.Seek(chunk.BasePosition, SeekOrigin.Begin);

                string finalDirectory = directory + @"\manifest\";
                string finalName = finalDirectory + fileName;
                Directory.CreateDirectory(finalDirectory);

                ChunkHandler.Dechunk(finalName, reader, (int)chunk.BaseLength);
            }
        }

        public void ExtractAllLoadedFiles(string targetDirectory)
        {
            foreach (var catalogue in catalogues)
            {
                foreach (var catEntry in catalogue.Files)
                {
                    try
                    {
                        WriteFileFromCatalogue(catEntry.Value, targetDirectory);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(catEntry.Value.ResolvedName + " " + catEntry.Value.Size + " " + catEntry.Value.Archive + " " + catEntry.Value.Offset);
                    }
                }
            }
        }

        public void ImportTextFile(string inputDirectory)
        {
            var chunk = LocateLanguageFileChunk();
            string compoundName = ResolveLanguageCasFile(chunk);

            using (BinaryWriter writer = new BinaryWriter(File.Open(compoundName, FileMode.Append)))
            {
                var offset = (int)writer.BaseStream.Position;
                int fileSize = ChunkHandler.Chunk(writer, inputDirectory + @"\manifest\" + EnglishChunkGuid);
                chunk.BasePosition = (uint)offset;
                chunk.BaseLength = (uint)fileSize;
            }

            UpdateManifest();
        }

        public void ImportFontFiles(string inputDirectory)
        {
            var fontPatchDescriptor = manifest.Patches.First(patch => patch.PatchIdentifier == EnglishFontDescriptorId);
            var manifestChunkDescriptorIndex = fontPatchDescriptor.PatchStart;
            var manifestChunk = manifest.Chunks[(int)manifestChunkDescriptorIndex];
            var chunkDescriptor = chunkDescriptors.First(desc => desc.Offset == manifestChunk.BasePosition);
            var fileLengthsMapping = new Queue<int>();
            var relativeChunkIdToFileNames = new Dictionary<int, List<string>>();

            using (StreamReader reader = new StreamReader(File.Open(inputDirectory + @"\fonts_import.descriptor", FileMode.Open)))
            {
                string line = null;
                int lineNumber = 1;
                while ((line = reader.ReadLine()) != null)
                {
                    var fileNames = new List<string>();

                    if (line != "-")
                    {
                        fileNames.AddRange(line.Split(','));
                    }

                    relativeChunkIdToFileNames[lineNumber++] = fileNames;
                }
            }

            var resourceDescriptorsToUpdate = new List<ResourceDescriptor>();

            using (BinaryWriter casWriter = new BinaryWriter(File.Open(BaseDirectory + DataDirs[1] + @"\cas_01.cas", FileMode.Append)))
            {
                for (int i = 1; i < fontPatchDescriptor.PatchLength; i++)
                {
                    if (relativeChunkIdToFileNames[i].Count > 0)
                    {
                        uint compressedBlockSize = 0;
                        var patchChunkDescriptor = manifest.Chunks[(int)manifestChunkDescriptorIndex + i];
                        patchChunkDescriptor.Archive = 0;
                        patchChunkDescriptor.DirectoryId = (byte)DirectoryNames.InitialExperiencePatch;
                        patchChunkDescriptor.BasePosition = (uint)casWriter.BaseStream.Position;

                        foreach (string fileName in relativeChunkIdToFileNames[i])
                        {
                            var compositeResourceDescriptor = chunkDescriptor.ResourceDescriptors.FirstOrDefault(descriptor => fileName.Contains(descriptor.Name));
                            var pathToNewFile = inputDirectory + @"\" + fileName;
                            compressedBlockSize += (uint)ChunkHandler.Chunk(casWriter, pathToNewFile);
                            compositeResourceDescriptor.Resource.Size = (int)new FileInfo(pathToNewFile).Length;
                            resourceDescriptorsToUpdate.Add(compositeResourceDescriptor.Resource);
                        }

                        patchChunkDescriptor.BaseLength = compressedBlockSize;
                    }
                }
            }

            using (BinaryWriter chunkDescriptorWriter = new BinaryWriter(File.Open(ResolveManifestCasFile(), FileMode.Open)))
            {
                foreach (var resourceDescriptor in resourceDescriptorsToUpdate)
                {
                    chunkDescriptorWriter.BaseStream.Seek(resourceDescriptor.Offset + 4, SeekOrigin.Begin);
                    chunkDescriptorWriter.WriteUInt32BE((UInt32)resourceDescriptor.Size);
                }
            }

            UpdateManifest();
        }

        private void UpdateManifest()
        {
            var catalogue = catalogues.Where(cat => cat.Files.ContainsKey(SessionManifestSha1)).First();
            CatalogueEntry manifestCatEntry = catalogue.Files[SessionManifestSha1];

            using (MemoryStream manifestMemoryStream = new MemoryStream())
            {
                using (ManifestBinaryWriter memoryManifestWriter = new ManifestBinaryWriter(manifestMemoryStream))
                {
                    memoryManifestWriter.Write(manifest);
                }

                byte[] manifestByteArray = manifestMemoryStream.ToArray();

                if (manifestByteArray.Length != manifestCatEntry.Size)
                {
                    throw new InvalidDataException("Incorrectly created manifest!");
                }

                byte[] updatedSha1 = CalculateSha1(manifestByteArray);

                using (BinaryWriter manifestWriter = new BinaryWriter(File.Open(ResolveManifestCasFile(), FileMode.Open)))
                {
                    manifestWriter.BaseStream.Seek(manifestCatEntry.Offset, SeekOrigin.Begin);
                    manifestWriter.Write(manifestByteArray);
                }

                using (BinaryWriter layoutWriter = new BinaryWriter(File.Open(BaseDirectory + LayoutTocPath, FileMode.Open)))
                {
                    layoutWriter.BaseStream.Seek(LayoutTocManifestSha1Location, SeekOrigin.Begin);
                    layoutWriter.Write(updatedSha1);
                }

                using (BinaryWriter catWriter = new BinaryWriter(File.Open(catalogue.Path, FileMode.Open)))
                {
                    catWriter.BaseStream.Seek(ManifestCatSha1Location, SeekOrigin.Begin);
                    catWriter.Write(updatedSha1);
                }
            }
        }

        private static void WriteToLengthsDescriptor(Queue<int> fileLengthsMapping, StreamWriter descriptorWriter, ref int currentTargetLength, ref int subtotalLength, IntermediateResourceDescriptor resourceDescriptor, string resourceName)
        {
            descriptorWriter.Write(resourceName);

            subtotalLength += resourceDescriptor.Size;
            if (subtotalLength == currentTargetLength)
            {
                descriptorWriter.Write("\n");
                subtotalLength = 0;
                currentTargetLength = fileLengthsMapping.Count > 0 ? fileLengthsMapping.Dequeue() : 0;
            }
            else
            {
                descriptorWriter.Write(",");
            }
        }

        private string BuildResourceName(string subDirectory, IntermediateResourceDescriptor descriptor)
        {
            return subDirectory + @"\" + descriptor.FinalResourceDescriptor.Name;
        }

        private static void ExtractFileFromIntermediateResourceDescriptor(string outputDirectory, MemoryStream completeDecompressedMemoryStream, IntermediateResourceDescriptor descriptor, string resourceName)
        {
            var outputName = outputDirectory + @"\" + resourceName;
            Directory.CreateDirectory(outputName.Substring(0, outputName.LastIndexOf('\\')));

            using (BinaryWriter writer = new BinaryWriter(File.Open(outputName, FileMode.Create)))
            {
                using (BinaryReader reader = new BinaryReader(completeDecompressedMemoryStream, ASCIIEncoding.ASCII, true))
                {
                    writer.Write(reader.ReadBytes(descriptor.Size));
                }
            }
        }

        private Chunk LocateLanguageFileChunk()
        {
            var englishDescriptor = manifest.Identifiers.Where(pair => BitConverter.ToString(pair.Id).Replace("-", "").ToLower() == EnglishChunkGuid).FirstOrDefault();
            return manifest.Chunks[(int)englishDescriptor.PatchStart];
        }

        private string ResolveLanguageCasFile(Chunk chunk)
        {
            return BaseDirectory + DataDirs[1] + @"\cas_" + (chunk.Archive + 1).ToString("00") + ".cas";
        }

        private string ResolveManifestCasFile()
        {
            return BaseDirectory + DataDirs[1] + @"\cas_27.cas";
        }

        private byte[] GetCurrentManifestSha1()
        {
            using (BinaryReader layoutReader = new BinaryReader(File.Open(BaseDirectory + LayoutTocPath, FileMode.Open)))
            {
                layoutReader.BaseStream.Seek(LayoutTocManifestSha1Location, SeekOrigin.Begin);
                return layoutReader.ReadBytes(20);
            }
        }

        private byte[] CalculateSha1(byte[] bytes)
        {
            byte[] output;

            using (SHA1 sha1 = new SHA1Managed())
            {
                output = sha1.ComputeHash(bytes);
            }

            return output;
        }

        private string GenerateRandomName(string path)
        {
            Random generator = new Random();
            return Path.ChangeExtension(path, @".tmp_" + generator.Next().ToString());
        }

        # region obsolete
        public void ExtractFontFilesThroughCatalogues(string outputDirectory)
        {
            var hashes = chunkDescriptors.SelectMany(desc => desc.Pairs.Where(pair => FontFilesIds.Contains(pair.Name))).Select(pair => pair.Hash).Distinct(new StructuralEqualityComparer());

            foreach (byte[] hash in hashes)
            {
                var catalogue = catalogues.Where(cat => cat.Files.ContainsKey(hash)).FirstOrDefault();
                if (catalogue != null)
                {
                    CatalogueEntry entry = catalogue.Files[hash];
                    entry.ResolvedName = BitConverter.ToString(hash).Replace("-", "");
                    WriteFileFromCatalogue(entry, outputDirectory);
                }
            }
        }

        public void ImportFontFilesThroughCatalogues(string inputDirectory)
        {
            var hashes = chunkDescriptors.SelectMany(desc => desc.Pairs.Where(pair => FontFilesIds.Contains(pair.Name))).Select(pair => pair.Hash).Distinct(new StructuralEqualityComparer());
            var cataloguesToOverwrite = new HashSet<Catalogue>();

            foreach (byte[] hash in hashes)
            {
                var catalogue = catalogues.Where(cat => cat.Files.ContainsKey(hash)).FirstOrDefault();
                if (catalogue != null)
                {
                    CatalogueEntry catEntry = catalogue.Files[hash];
                    catEntry.ResolvedName = BitConverter.ToString(hash).Replace("-", "");

                    string compoundName = Path.GetDirectoryName(catEntry.Parent.Path) + @"\\" + "cas_" + catEntry.Archive.ToString().PadLeft(2, '0') + ".cas";

                    using (BinaryWriter writer = new BinaryWriter(File.Open(compoundName, FileMode.Append)))
                    {
                        catEntry.Offset = (int)writer.BaseStream.Position;
                        int fileSize = ChunkHandler.Chunk(writer, inputDirectory + @"\cat\" + catEntry.ResolvedName);
                        catEntry.Size = fileSize;
                    }
                }

                cataloguesToOverwrite.Add(catalogue);
            }

            foreach (var catalogue in cataloguesToOverwrite)
            {
                string newCataloguePath = catalogue.Path + "_tmp";

                using (CatalogueBinaryWriter writer = new CatalogueBinaryWriter(File.Open(newCataloguePath, FileMode.Create)))
                {
                    writer.Write((byte)0x01, catalogue);
                }
            }
        }
        # endregion obsolete
    }
}