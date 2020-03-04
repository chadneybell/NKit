using System;

namespace Nanook.NKit
{
    internal class WiiFillerSectionItem : BaseSection
    {
        private readonly JunkStream _junk;
        private readonly byte[] _junkData;
        private readonly bool _useBuff;

        internal WiiFillerSectionItem(NStream stream, long discOffset, byte[] data, long size, bool useBuff, JunkStream junk) : base(stream, discOffset, data, size)
        {
            _useBuff = useBuff;
            _junk = junk;
            if (_junk != null)
            {
                _junk.Position = discOffset;
                _junkData = new byte[Data.Length];
                _junk.Read(_junkData, 0, (int)base.Size);
                Array.Clear(_junkData, 0, 28);
                base.Data = _useBuff ? data : _junkData;
            }
        }

        public void Populate(byte[] data, long discOffset, long size)
        {
            base.DiscOffset = discOffset;
            base.Size = size;
            if (_junk != null)
            {
                _junk.Read(_junkData, 0, (int)base.Size);
            }

            base.Data = _useBuff ? data : _junkData;
        }

        public byte[] Junk => _junkData;
    }
}
