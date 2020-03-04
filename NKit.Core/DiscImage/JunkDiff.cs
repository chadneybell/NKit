﻿namespace Nanook.NKit
{
    internal class JunkDiff
    {
        public JunkDiff(long offset, long length)
        {
            Offset = offset;
            Length = length;
            Type = null;
        }

        public long Offset { get; internal set; }
        public long Length { get; internal set; }
        public byte? Type { get; internal set; }
        public long Stored { get; internal set; }
    }
}
