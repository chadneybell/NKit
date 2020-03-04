using System;

namespace Nanook.NKit
{
    public enum MatchType { Redump, Custom, MatchFail }
    public enum Region { Japan = 0, Usa = 1, Pal = 2, Korea = 4 }
    public enum DiscType { Wii = 0, GameCube = 1 }
    public enum VerifyResult { Unverified = 0, VerifySuccess = 1, VerifyFailed = 2, Error = 3 }

    public class OutputResults
    {
        public OutputResults()
        {

        }

        public DiscType DiscType { get; internal set; }

        public string ProcessorMessage { get; internal set; }

        public string Conversion { get; internal set; }
        public RedumpInfo RedumpInfo { get; internal set; }

        public string InputTitle { get; internal set; }
        public string OutputTitle { get; internal set; }
        public string InputId4 => InputId8?.Substring(0, 4);
        public string OutputId4 => OutputId8?.Substring(0, 4);
        public string InputId6 => InputId8?.Substring(0, 6);
        public string OutputId6 => OutputId8?.Substring(0, 6);
        public string InputId8 { get; internal set; }
        public string OutputId8 { get; internal set; }
        public int InputDiscNo { get; internal set; }
        public int OutputDiscNo { get; internal set; }
        public int InputDiscVersion { get; internal set; }
        public int OutputDiscVersion { get; internal set; }
        public string AliasJunkId { get; internal set; }
        public long InputSize { get; internal set; }
        public long OutputSize { get; internal set; }
        public uint OutputPrePatchCrc { get; internal set; }
        public uint OutputCrc { get; internal set; }
        public byte[] OutputMd5 { get; internal set; }
        public byte[] OutputSha1 { get; internal set; }
        public uint ValidationCrc { get; internal set; }
        public uint VerifyCrc { get; internal set; }
        /// <summary>
        /// True if testing not required or or tested and passed
        /// </summary>
        public VerifyResult VerifyOutputResult { get; internal set; }
        public VerifyResult ValidateReadResult { get; internal set; }
        public string OutputFileName { get; internal set; }
        public string OutputFileExt { get; internal set; }
        public string InputFileName { get; internal set; }
        public TimeSpan ProcessingTime { get; internal set; }
        public string ErrorMessage { get; internal set; }
        public long FullSize { get; internal set; }
        public string Passes { get; internal set; }
        public bool IsRecoverable { get; internal set; }
    }

    public class RedumpInfo
    {
        public string MatchName { get; internal set; } //Compatible Name
                                                       //public bool IsRestorable { get; internal set; }
                                                       //public MatchType MatchType { get; internal set; }
        public ChecksumsResult Checksums { get; internal set; }
        public MatchType MatchType { get; internal set; }
    }

    public class NkitInfo
    {
        public long BytesData { get; internal set; } //data and gaps should sum to source disc size
        public long BytesGaps { get; internal set; }
        public long BytesHashesData { get; internal set; }
        public long BytesHashesPreservation { get; internal set; }
        public long BytesPreservationData { get; internal set; } //Write Gaps Size
        public long BytesPreservationDiscPadding { get; internal set; } //Write Gaps Size
        public long BytesJunkFiles { get; internal set; } //Write data is BytesData - BytesJunkfiles
        public long BytesReadSize => BytesData + BytesGaps;
        public long BytesWriteSize => (BytesData - BytesJunkFiles) + BytesPreservationData + BytesPreservationDiscPadding;
        public long BytesGcz { get; internal set; }
        public long FilesTotal { get; internal set; }
        public long FilesAligned { get; internal set; }
    }


}
