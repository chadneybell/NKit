using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Nanook.NKit
{
    internal class WiiPartitionGroupSection : BaseSection
    {
        public bool IsEncrypted { get; private set; } //reset as encrypted

        private readonly WiiPartitionGroupEncryptionState _data;

        private int _idx;
        private readonly int _maxLength;
        private readonly byte[] _h3Table;
        private readonly WiiPartitionHeaderSection _partHdr;
        private readonly bool _isIsoDec;

        public WiiPartitionHeaderSection Header => _partHdr;

        public int H3Errors { get; private set; }

        public byte[] Key => _partHdr.Key;
        internal long DataOffset { get; private set; }
        internal long Offset { get; private set; }
        public byte[] Junk { get; private set; }
        public byte[] Encrypted => _data.Encrypted;
        public byte[] Decrypted => _data.Decrypted;
        public PartitionType Type => _partHdr.Type;

        private JunkStream _junk;
        private readonly bool[] _unscrubValid;

        internal WiiPartitionGroupSection(WiiDiscHeaderSection hdr, WiiPartitionHeaderSection partHdr, byte[] data, long discOffset, long size, bool encrypted) : this(null, hdr, partHdr, data, discOffset, size, encrypted)
        {

        }

        public override byte[] Data
        {
            get => base.Data;
            protected set => base.Data = value;
        }

        internal WiiPartitionGroupSection(NStream stream, WiiDiscHeaderSection hdr, WiiPartitionHeaderSection partHdr, byte[] data, long discOffset, long size, bool encrypted) : base(stream, discOffset, new byte[(0x400 * 32) * 64], size)
        {
            _isIsoDec = hdr.IsIsoDecPartition(partHdr.DiscOffset);
            _partHdr = partHdr;
            _h3Table = _partHdr.H3Table;
            _idx = 0;
            _maxLength = base.Data.Length; //0x8000 per block * 64 = 0x200000

            _data = new WiiPartitionGroupEncryptionState((int)WiiPartitionSection.GroupSize, Key, _h3Table);

            IsEncrypted = encrypted || !data.Equals(0x26c, new byte[20], 0, 20);
            Junk = new byte[WiiPartitionSection.GroupSize];

            _unscrubValid = new bool[64];
            _data.Populate(data, (int)size, IsEncrypted && !_isIsoDec, _isIsoDec, _idx);

            initialise();
        }

        public void Populate(int groupIdx, byte[] data, long discOffset, long size)
        {
            base.DiscOffset = discOffset;
            base.Size = size;
            _idx = groupIdx;

            _data.Populate(data, (int)size, IsEncrypted && !_isIsoDec, _isIsoDec, _idx);

            initialise();
        }

        private void initialise()
        {
            Offset = _idx * (long)_maxLength;
            DataOffset = _idx * 64L * 0x7c00L;
            H3Errors = 0;
        }

        public bool PreserveHashes()
        {
            int scrubbedBlocks = _data.ScrubbedBlocks;
            if (scrubbedBlocks != 0 && scrubbedBlocks < _data.UsedBlocks)
            {
                return true;
            }

            long end = DataOffset + (64 * 0x7c00L);
            bool usedScrubbed = scrubbedBlocks == _data.UsedBlocks && (Header.FileSystem != null && Header.FileSystem.Files.Any(a =>
                                                                       (DataOffset <= a.DataOffset && end > a.DataOffset) //file starts in this group
                                                                    || (DataOffset <= a.DataOffset + a.Length && end > a.DataOffset + a.Length) //file ends in this group
                                                                    || (DataOffset >= a.DataOffset && end <= a.DataOffset + a.Length))); //in the middle of a file
            if (usedScrubbed)
            {
                return true;
            }

            if (scrubbedBlocks == _data.UsedBlocks)
            {
                return !_data.AllScrubbedSameByte();
            }

            bool preserve = !_data.FastHashIsValid();
            return preserve;
        }

        private int dataCopy(int position, int length, bool encrypted, Stream stream, byte[] buffer, int bufferOffset)
        {
            int c = 0;
            int b = position / 0x7c00;
            int p = position % 0x7c00;
            int l;
            while (b < _data.UsedBlocks && c != length)
            {
                if ((l = Math.Min(0x7c00 - p, length - c)) == 0)
                {
                    break;
                }

                if (buffer != null)
                {
                    Array.Copy(_data.Decrypted, ((b++ * 0x8000) + 0x400) + p, buffer, bufferOffset + c, l);
                }

                if (stream != null)
                {
                    stream.Write(_data.Decrypted, ((b++ * 0x8000) + 0x400) + p, l);
                }

                c += l;
                p = 0;
            }
            return c; //bytes copied
        }

        public int DataCopy(int position, int length, bool encrypted, byte[] buffer, int bufferOffset)
        {
            return dataCopy(position, length, encrypted, null, buffer, bufferOffset);
        }

        public int DataCopy(int position, int length, bool encrypted, Stream stream)
        {
            return dataCopy(position, length, encrypted, stream, null, 0);
        }

        internal void MarkBlockDirty(int blockIndex)
        {
            _data.MarkBlockDirty(blockIndex);
        }

        internal void SetScrubbed(int blockIndex, byte scrubByte)
        {
            _data.MarkBlockScrubbed(blockIndex, scrubByte);
        }

        private bool areaUsed(FstFile file, long offset, long size)
        {
            bool b = (file.DataOffset > offset && file.DataOffset < offset + size); //starts within range
            if (b)
            {
                return true;
            }

            b = (file.DataOffset + file.Length > offset && file.DataOffset + file.Length < offset + size); //ends within range
            if (b)
            {
                return true;
            }

            b = (file.DataOffset < offset && file.DataOffset + file.Length > offset + size);
            if (b)
            {
                return true;
            }

            return false;
        }

        public bool Unscrub(List<JunkRedumpPatch> junkPatches)
        {
            bool changed = false;
            //bool performCheck = true; //start on the assumption last was valid as it would be hassle to work out
            List<FstFile> nulls = new List<FstFile>();

            bool good = _data.IsValid(false); //forces decrypt and hash cache build and test - does not test data in blocks matchs H0 table
            Parallel.For(0, _data.UsedBlocks, bi => _unscrubValid[bi] = _data.BlockIsValid(bi)); //test data matches H0 table

            for (int bi = 0; bi < _data.UsedBlocks; bi++)
            {
                if (!_unscrubValid[bi])
                {
                    if (_junk == null)
                    {
                        _junk = new JunkStream(_partHdr.Id, _partHdr.DiscNo, _partHdr.PartitionDataSize);
                    }

                    _junk.Position = NStream.OffsetToData(Offset + _data.BlockDataOffset(bi), true);
                    _junk.Read(_data.Decrypted, _data.BlockDataOffset(bi), 0x7c00);
                    _data.MarkBlockUnscrubbedAndDirty(bi);
                    changed = true;
                }
            }

            if (junkPatches != null && junkPatches.Count != 0)
            {
                foreach (JunkRedumpPatch jp in junkPatches)
                {
                    if (jp.Offset >= DiscOffset && jp.Offset < DiscOffset + Size)
                    {
                        Array.Copy(jp.Data, 0, _data.Decrypted, jp.Offset - DiscOffset, jp.Data.Length);
                        _data.MarkBlockDirty((int)((jp.Offset - DiscOffset) / 0x8000));  //560de532
                    }
                }
            }

            if (changed)
            {
                good = _data.IsValid(true); //true as changes were made
                if (!good)
                {
                    bool zerod = false;
                    List<Tuple<long, int, FstFile>> h3Nulls = new List<Tuple<long, int, FstFile>>(_partHdr.ScrubManager.H3Nulls);

                    if (h3Nulls.Count != 0)
                    {
                        int dataLen = (int)NStream.HashedLenToData(Decrypted.Length);
                        foreach (Tuple<long, int, FstFile> n in h3Nulls)
                        {
                            if (n.Item1 >= DataOffset && n.Item1 < DataOffset + dataLen)
                            {
                                int idx = (int)((n.Item1 - DataOffset) / 0x7c00);
                                _data.MarkBlockDirty(idx);
                                int pos = (int)NStream.DataToOffset(n.Item1 - DataOffset, true);
                                Array.Clear(_data.Decrypted, pos, n.Item2);
                                zerod = true;
                            }
                        }
                    }
                    if (zerod)
                    {
                        good = _data.IsValid(true);
                    }

                    if (!good)
                    {
                        H3Errors++;
                    }
                }
            }

            return changed;
        }

        internal bool IsValid(bool calculateHashes)
        {
            return _data.IsValid(calculateHashes);
        }

        internal void ForceHashes(byte[] hashes)
        {
            _data.ForceHashes(hashes);
        }

    }
}
