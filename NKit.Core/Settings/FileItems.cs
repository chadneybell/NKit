using System.IO;

namespace Nanook.NKit
{
    public class FileItem
    {
        public FileItem(string filename, long length, uint crc)
        {
            Filename = filename;
            Length = length;
            Crc = crc;
        }
        public string Filename { get; }
        public long Length { get; }
        public uint Crc { get; }
    }

    public class ApploaderFileItem : FileItem
    {
        public ApploaderFileItem(string filename, long length, uint crc) : base(filename, length, crc)
        {
            CalcCrc = 0;
        }

        public uint CalcCrc { get; set; }
    }

    public class FstFileItem : FileItem
    {
        public FstFileItem(string filename, long length, uint crc, string id8, uint appLoadCrc, uint postFstCrc) : base(filename, length, crc)
        {
            Id8 = id8;
            AppLoadCrc = appLoadCrc;
            PostFstCrc = postFstCrc;
        }

        public void Populate()
        {
            if (!string.IsNullOrEmpty(Filename) && File.Exists(Filename))
            {
                MemorySection ms = new MemorySection(File.ReadAllBytes(Filename));
                MainDolOffset = ms.ReadUInt32B(0x00);
                FstOffset = ms.ReadUInt32B(0x04);
                MaxFst = ms.ReadUInt32B(0x08);
                Region = (Region)ms.ReadUInt32B(0x0C);
                Title = ms.Read(0x10, 0x50 - 0x10);
                FstData = ms.Read(0x50, (int)ms.Size - 0x50);
            }
        }

        public string Id8 { get; }
        public uint AppLoadCrc { get; }
        public uint PostFstCrc { get; }

        public long FstOffset { get; private set; }
        public long MainDolOffset { get; private set; }
        public Region Region { get; private set; }
        public long MaxFst { get; private set; }
        public byte[] Title { get; private set; }
        public byte[] FstData { get; private set; }
    }
}
