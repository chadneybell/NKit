using System;
using System.IO;

namespace Nanook.NKit
{
    internal class VerifyWriter : IWriter
    {
        private ILog _log;
        public void Construct(ILog log)
        {
            _log = log;
            VerifyIsWrite = true;
        }

        public bool VerifyIsWrite { get; set; }
        public bool RequireVerifyCrc { get; set; }
        public bool RequireValidationCrc { get; set; }

        public void Write(Context ctx, Stream inStream, Stream output, Coordinator pc)
        {
            try
            {
                long imageSize = pc.OutputSize;
                pc.WriterCheckPoint1WriteReady(out string junkId); //wait until read has written the header and set the length

                inStream.Copy(Stream.Null, imageSize);

                pc.WriterCheckPoint2Complete(out NCrc readerCrcs, out uint validationCrc, null, imageSize); //wait until reader has completed and get crc patches.

                uint fullCrc = readerCrcs.FullCrc(true);
                string msg;
                if (validationCrc == 0)
                {
                    msg = string.Format("Crc:{0} - No Test Crc Found", fullCrc.ToString("X8"));
                }
                else if (validationCrc == fullCrc)
                {
                    msg = string.Format("Crc:{0} - Success", fullCrc.ToString("X8"));
                }
                else
                {
                    msg = string.Format("Crc:{0} - Failed Test Crc:{1}", fullCrc.ToString("X8"), validationCrc.ToString("X8"));
                }

                pc.WriterCheckPoint3ApplyPatches(null, false, fullCrc, fullCrc, VerifyIsWrite, msg);
            }
            catch (Exception ex)
            {
                throw pc.SetWriterException(ex, "VerifyWriter.Write - Image Write");
            }
        }
    }
}