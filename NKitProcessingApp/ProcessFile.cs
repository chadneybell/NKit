﻿namespace Nanook.NKit
{
    internal class ProcessFile
    {
        public SourceFile SourceFile { get; set; }
        public OutputResults Results { get; set; }
        public string Log { get; set; }

        public override string ToString()
        {
            return SourceFile?.Name ?? "";
        }
    }

}
