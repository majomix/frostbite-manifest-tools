using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostbiteManifestSystemTools.Model
{
    public class CompositeResourceDescriptor
    {
        public string Name { get; set; }
        public ResourceDescriptor Ebx { get; set; }
        public ResourceDescriptor Resource { get; set; }
    }

    public class ResourceDescriptor
    {
        public long Offset { get; set; }
        public int Size { get; set; }
    }

    public class IntermediateResourceDescriptor
    {
        public long FileOffset { get; set; }
        public int Size { get; set; }
        public int NameOffset { get; set; }
        public CompositeResourceDescriptor FinalResourceDescriptor { get; set; }
    }
}
