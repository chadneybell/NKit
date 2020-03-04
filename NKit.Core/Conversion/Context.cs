using System.IO;
using System.Linq;

namespace Nanook.NKit
{
    internal class Context
    {
        internal Context()
        {
        }

        public void Initialise(string conversionName, SourceFile file, Settings settings, bool scanNewBins, bool isRecovery, bool isGameCube, string id8, ILog log)
        {
            //this.NStream = file.OpenNStream();
            ConversionName = conversionName;
            Settings = settings;

            DatData data = new DatData(Settings, file.Index == 0 ? log : null);
            RecoveryData rec = null;

            if (isRecovery)
            {
                rec = new RecoveryData(Settings, file.Index == 0 ? log : null, isGameCube, id8);
            }

            int fileTotalLen = file.TotalFiles.ToString().Length;
            log?.Log(string.Format("#####[ {0} / {1} ]{2}", (file.Index + 1).ToString().PadLeft(fileTotalLen), file.TotalFiles.ToString(), new string('#', 79 - ((fileTotalLen * 2) + 12))));
            log?.LogBlank();
            log?.Log("FILES");
            log?.Log("-------------------------------------------------------------------------------");
            log?.Log(string.Format("Input: {1}", file.IsArchive ? "Archive" : "Input", Path.GetDirectoryName(file.FilePath)));
            if (file.AllFiles.Length != 0)
            {
                //log?.LogDetail(string.Format("{0} Files:", file.IsArchive ? "Archive" : "Input"));
                foreach (string nm in file.AllFiles.Select(a => Path.GetFileName(a)))
                {
                    log?.Log("    " + nm);
                }
            }
            else
            {
                log?.Log(string.Format("  {1}", file.IsArchive ? "Archive" : "Input", Path.GetFileName(file.FilePath)));
            }

            if (file.IsArchive)
            {
                if (!string.IsNullOrEmpty(file.Path))
                {
                    log?.Log(file.Path);
                }

                log?.Log("  " + file.Name);
            }
            log?.LogBlank();
            log?.Log("Temp:  " + Path.GetDirectoryName(Settings.TempPath));
            if (Settings.EnableSummaryLog)
            {
                log?.Log(string.Format("SmLog: {0}", Settings.SummaryLog));
            }

            Dats = data;
            Recovery = rec;
            log?.LogBlank();

        }

        public Settings Settings { get; private set; }
        public DatData Dats { get; private set; }
        public RecoveryData Recovery { get; private set; }
        public uint ValidationCrc { get; set; }
        public string ConversionName { get; private set; }

#if DEBUG
        public int NkitNonmatchBlocks { get; set; } //testing for edge cases
#endif

    }


}
