using SharpCompress.Archives;
using System;
using System.IO;
using System.Linq;

namespace Nanook.NKit
{
    /// <summary>
    /// Supports Multiple file archives and split (.001 .002 / wbfs wbf1 etc) files. Split files in archives is not supported. Archives within archives is not supported
    /// </summary>
    public class SourceFile
    {
        /// <summary>
        /// File name (may be within an archive)
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// Path of file (within archive or in file system)
        /// </summary>
        public string Path { get; internal set; }
        /// <summary>
        /// File name of archive
        /// </summary>
        public string FilePath { get; internal set; }

        /// <summary>
        /// All path and filenames for multipart and split sets
        /// </summary>
        public string[] AllFiles { get; internal set; }

        /// <summary>
        /// True if the file is split (not multipart). This library will preset the files as one stream
        /// </summary>
        public bool IsSplit { get; internal set; }

        /// <summary>
        /// Is the Name and Path inside an archive (making FilePath the physical file)
        /// </summary>
        public bool IsArchive { get; internal set; }

        public long Length { get; internal set; }

        public int Index { get; internal set; }
        public int TotalFiles { get; internal set; }

        public SourceStream OpenStream()
        {
            return new SourceStream(this);
        }

        public string CreateOutputFilename(string newExtension)
        {
            if (!IsArchive)
            {
                return SourceFiles.ChangeExtension(FilePath, false, newExtension.TrimStart('.'));
            }
            else
            {
                return SourceFiles.ChangeExtension(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(FilePath), Name), false, newExtension.TrimStart('.'));
            }
        }

        public NStream OpenNStream()
        {
            return OpenNStream(true);
        }
        public NStream OpenNStream(bool readAsDisc)
        {

            if (!File.Exists(FilePath))
            {
                throw new HandledException("SourceFile.Open - '{0}' does not exist.", (FilePath) ?? "");
            }

            Stream fs = null;

            if (!IsArchive)
            {
                try
                {
                    fs = OpenStream();
                    NStream nStream = new NStream(new StreamForward(fs, null));
                    nStream.Initialize(readAsDisc);
                    return nStream;
                }
                catch (Exception ex)
                {
                    throw new HandledException(ex, "SourceFile.OpenNStream '{0}'", FilePath ?? "");
                }
            }
            else
            {
                string arcType = "";
                IArchive archive;
                if (!IsSplit)
                {
                    try
                    {
                        arcType = ((AllFiles.Length == 0) ? "" : "multipart ") + "archive";
                        archive = ArchiveFactory.Open(FilePath); //handles multipart archives
                    }
                    catch (Exception ex)
                    {
                        throw new HandledException(ex, "SourceFile.OpenNStream ({0}) '{1}'", arcType, FilePath ?? "");
                    }
                }
                else
                {
                    try
                    {
                        arcType = "split archive";
                        fs = OpenStream();
                        archive = ArchiveFactory.Open(fs); //handles multipart archives
                    }
                    catch (Exception ex)
                    {
                        throw new HandledException(ex, "SourceFile.OpenNStream ({0}) '{1}'", arcType, FilePath ?? "");
                    }
                }

                IArchiveEntry ent;
                string key = "";
                try
                {
                    //zip uses / path separator rar uses \
                    int pathLen = string.IsNullOrEmpty(Path) ? 0 : Path.Length + 1;
                    ent = archive.Entries.FirstOrDefault(e => e.Key.Length == pathLen + Name.Length && e.Key.StartsWith(Path) && e.Key.EndsWith(Name));
                    if (ent == null)
                    {
                        throw new Exception("Open archive file entry failure");
                    }

                    key = ent.Key;
                }
                catch (Exception ex)
                {
                    throw new HandledException(ex, "SourceFile.OpenNStream ({0}) '{1}' failed to open entry '{2}'", arcType, FilePath ?? "", Name ?? "");
                }

                try
                {

                    if (ent != null)
                    {
                        NStream nStream = new NStream(new StreamForward(ent.Size, ent.OpenEntryStream(), archive));
                        nStream.Initialize(true);
                        return nStream;
                    }
                }
                catch (Exception ex)
                {
                    throw new HandledException(ex, "SourceFile.OpenNStream ({0}) '{1}' failed to stream entry '{2}'", arcType, FilePath ?? "", key ?? "");
                }

                return null;
            }


        }

        internal bool Exists(string fileOut)
        {
            throw new NotImplementedException();
        }
    }
}
