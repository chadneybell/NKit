using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nanook.NKit
{
    internal class WiiFillerSection : IWiiDiscSection
    {
        private readonly NStream _stream;
        private readonly byte[] _buff;
        private readonly string _junkId;
        public long DiscOffset { get; private set; }
        public long Size { get; private set; }
        private readonly long _srcSize;
        private readonly bool _generateUpdateFiller;
        private readonly bool _generateOtherFiller;
        private readonly bool _forceFillerJunk;
        private readonly bool _updatePartiton;

        internal WiiFillerSection(NStream stream, bool updatePartition, long discOffset, long size, long updateSkip, string overrideJunkId, bool generateUpdateFiller, bool generateOtherFiller, bool forceFillerJunk)
        {
            _stream = stream;
            _buff = new byte[0x40000]; //junkstream block size
            _junkId = overrideJunkId ?? stream.Id;
            DiscOffset = discOffset;
            Size = size;
            _srcSize = size - updateSkip;
            _generateUpdateFiller = generateUpdateFiller || size > _srcSize;
            _generateOtherFiller = generateOtherFiller;
            _forceFillerJunk = forceFillerJunk;
            _updatePartiton = updatePartition;
        }

        public IEnumerable<WiiFillerSectionItem> Sections
        {
            get
            {
                bool ffScrubbedUpdate = false;
                WiiDiscHeaderSection hdr = (WiiDiscHeaderSection)_stream.DiscHeader;
                bool readImg = (_updatePartiton && !_generateUpdateFiller) || (!_updatePartiton && !_generateOtherFiller);
                bool createJunk = (!_updatePartiton && (_generateOtherFiller || _forceFillerJunk));

                _stream.ChangeJunk(0, _junkId, hdr.DiscNo, _stream.RecoverySize);

                int len = (int)Math.Min(_buff.Length, Size);
                if (readImg)
                {
                    _stream.Read(_buff, 0, len); //read 1 section
                }
                else
                {
                    if (hdr.Partitions.Any(a => a.DiscOffset > DiscOffset)) //if no more partitions then we can stop using the source stream
                    {
                        _stream.Seek(_srcSize, SeekOrigin.Current); //skip all as sections
                    }
                }

                ffScrubbedUpdate = _updatePartiton && _buff.Equals(0, len, 0xFF); //FF scrubbed - then update partition needs to be swapped for 00
                if (ffScrubbedUpdate)
                {
                    Array.Clear(_buff, 0, len);
                }

                WiiFillerSectionItem es = new WiiFillerSectionItem(_stream, DiscOffset, _buff, len, readImg || _updatePartiton, createJunk ? _stream.JunkStream : null);
                yield return es;
                WiiFillerSectionItem last = es;
                while (last.DiscOffset + last.Size < DiscOffset + Size)
                {
                    len = (int)Math.Min(_buff.Length, (DiscOffset + Size) - (last.DiscOffset + last.Size));
                    if (readImg)
                    {
                        _stream.Read(_buff, 0, len);
                    }

                    if (ffScrubbedUpdate)
                    {
                        Array.Clear(_buff, 0, len);
                    }

                    es.Populate(_buff, last.DiscOffset + last.Size, len);
                    yield return es;
                    last = es;
                }
            }
        }
    }
}
