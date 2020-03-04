using System;
using System.IO;

namespace Nanook.NKit
{
    internal class WiiPartitionPlaceHolder : WiiPartitionInfo, IDisposable
    {
        private WiiPartitionSection _reader;
        private readonly bool _isPlaceholder;
        private readonly NStream _nStream;
        private NStream _ws;

        public WiiPartitionPlaceHolder(NStream nStream, string filename, PartitionType type, long offset, int table) : base(type, offset, table, 0)
        {
            _nStream = nStream;
            Filename = filename;
            _isPlaceholder = true;
        }

        public WiiPartitionPlaceHolder(NStream nStream, PartitionType type, long offset, int table) : base(type, offset, table, 0)
        {
            _nStream = nStream;
            Filename = null;
            _isPlaceholder = false;
        }

        public bool IsPlaceholder => _isPlaceholder;

        public string Filename { get; set; }
        public long FileLength => new FileInfo(Filename).Length;

        public NStream Stream
        {
            get
            {
                if (Filename != null)
                {
                    _ws = new NStream(File.OpenRead(Filename));
                    _ws.Initialize(false);
                }
                return _ws;
            }
        }


        internal WiiPartitionSection Reader
        {
            get
            {
                if (_reader == null && Filename != null)
                {
                    _ws = new NStream(File.OpenRead(Filename));
                    _ws.Initialize(false);
                    _reader = new WiiPartitionSection(_nStream, (WiiDiscHeaderSection)_nStream.DiscHeader, _ws, 0);
                }
                return _reader;
            }
        }
        public override string ToString()
        {
            return string.Format("{0}", DiscOffset.ToString("X8"));
        }
        public void Dispose()
        {
            try
            {
                if (_nStream != _ws)
                {
                    _ws.Close();
                }
            }
            catch { }
        }
    }
}
