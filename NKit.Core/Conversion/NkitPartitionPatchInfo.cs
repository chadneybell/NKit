using System.Collections.Generic;

namespace Nanook.NKit
{
    internal class NkitPartitionPatchInfo
    {
        public long DiscOffset;
        public long Size => PartitionHeader == null ? 0 : (PartitionHeader.ReadUInt32B(0x2bc) * 4L);
        public Dictionary<long, MemorySection> HashGroups;
        public ScrubManager ScrubManager;
        public MemorySection PartitionHeader;
        public MemorySection PartitionDataHeader;
        public MemorySection Fst;
    }
}
