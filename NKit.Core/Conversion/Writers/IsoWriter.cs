using System;
using System.IO;

namespace Nanook.NKit
{
    internal class IsoWriter : IWriter
    {
        private ILog _log;
        public void Construct(ILog log)
        {
            _log = log;
        }
        public bool VerifyIsWrite { get; set; }
        public bool RequireVerifyCrc { get; set; }
        public bool RequireValidationCrc { get; set; }

        public void Write(Context ctx, Stream inStream, Stream outStream, Coordinator pc)
        {
            try
            {
                long imageSize = pc.OutputSize;
                pc.WriterCheckPoint1WriteReady(out string ignoreJunkId); //wait until read has written the header and set the length
                inStream.Copy(outStream, imageSize);

                pc.WriterCheckPoint2Complete(out NCrc readerCrcs, out uint validationCrc, null, imageSize); //wait until reader has completed and get crc patches.

                uint crc = readerCrcs?.FullCrc(true) ?? 0;
                pc.WriterCheckPoint3ApplyPatches(null, false, crc, crc, VerifyIsWrite, null);

            }
            catch (Exception ex)
            {
                throw pc.SetWriterException(ex, "IsoWriter.Write - Image Write");
            }
        }
    }
}