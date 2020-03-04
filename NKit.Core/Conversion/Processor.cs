using System;
using System.IO;
using System.Linq;
using System.Timers;

namespace Nanook.NKit
{
    public enum ProcessorSizeMode { Source, Stream, Image, Recover }

    internal class Processor
    {
        public IReader Reader;
        public IWriter Writer;

        private Timer _timer;
        private bool _timerRunning; //prevent 2 running at once - when user selects text in console fix

        private readonly ILog _log;
        private readonly ProcessorSizeMode _sizeMode;

        public Processor(IReader reader, IWriter writer, string title, ILog log, bool readerCanLog, bool writerCanLog, ProcessorSizeMode sizeMode)
        {
            _log = log;
            _sizeMode = sizeMode;
            Reader = reader;
            Writer = writer;
            Reader.Construct(readerCanLog ? _log : null);
            Writer.Construct(writerCanLog ? _log : null);
            Title = title;
        }

        public bool IsVerify => Writer is VerifyWriter || Writer is HashWriter;
        public bool IsCompressor => Writer is GczWriter;


        public bool HasWriteStream => !(Writer is VerifyWriter || Writer is HashWriter || Writer == null);

        public string Title { get; set; }

        public virtual OutputResults Process(Context ctx, NStream input, Stream output)
        {
            OutputResults results = new OutputResults() { Conversion = ctx.ConversionName, VerifyOutputResult = VerifyResult.Unverified, ValidateReadResult = VerifyResult.Unverified };
            try
            {
                StreamCircularBuffer fs = null;

                _timer = new Timer
                {
                    Interval = 250
                };
                _timer.Elapsed += (s, e) =>
                {
                    if (_timerRunning)
                    {
                        return; //keep processing
                    }

                    try
                    {
                        _timerRunning = true;
                        _log.ProcessingProgress(((IProgress)fs)?.Value ?? 0);
                    }
                    catch { }
                    finally
                    {
                        _timerRunning = false;
                    }
                };
                _timer.Enabled = true;

                long size;
                switch (_sizeMode)
                {
                    case ProcessorSizeMode.Source:
                        size = input.SourceSize;
                        break;
                    case ProcessorSizeMode.Stream:
                        size = input.Length;
                        break;
                    case ProcessorSizeMode.Image:
                        size = input.ImageSize;
                        break;
                    case ProcessorSizeMode.Recover:
                    default:
                        size = input.RecoverySize;
                        break;
                }

                Coordinator pc = new Coordinator(ctx.ValidationCrc, Reader, Writer, size);

                pc.Started += (s, e) =>
                {
                    _timer.Enabled = true;
                    results.AliasJunkId = e.AliasJunkId;
                };
                pc.Completed += (s, e) =>
                {
                    _timer.Enabled = false;
                    if (Writer is HashWriter)
                    {
                        results.OutputMd5 = e.Md5;
                        results.OutputSha1 = e.Sha1;
                    }
                    else
                    {
                        MemorySection hdr = new MemorySection(e.Header ?? input.DiscHeader.Data);
                        results.OutputTitle = hdr.ReadStringToNull(0x20, 0x60);
                        results.OutputDiscNo = hdr.Read8(6);
                        results.OutputDiscVersion = hdr.Read8(7);
                        results.OutputId8 = string.Concat(hdr.ReadString(0, 6), results.OutputDiscNo.ToString("X2"), results.OutputDiscVersion.ToString("X2"));
                        results.ProcessorMessage = e.ResultMessage;
                        results.OutputCrc = e.PatchedCrc;
                        results.IsRecoverable = e.IsRecoverable;
                        if (e.ValidationCrc != 0)
                        {
                            results.ValidationCrc = e.ValidationCrc;
                            results.ValidateReadResult = e.ValidationCrc == e.PatchedCrc ? VerifyResult.VerifySuccess : VerifyResult.VerifyFailed;
                        }

                        if (!(Writer is VerifyWriter))
                        {
                            results.OutputSize = e.OutputSize; //never store the verify size
                        }
                        else
                        {
                            results.VerifyCrc = e.VerifyCrc;
                            if (e.VerifyIsWrite) //e.ValidationCrc can be set from a previous process run
                            {
                                results.VerifyOutputResult = results.ValidationCrc == results.VerifyCrc ? VerifyResult.VerifySuccess : VerifyResult.VerifyFailed;
                            }
                        }

                    }

                    bool l9 = pc.Patches.Crcs.Any(a => a.Offset > 0xFFFFFFFFL || a.Length > 0xFFFFFFFFL);
                    if (pc.ReaderCrcs != null)
                    {
                        foreach (CrcItem c in pc.ReaderCrcs.Crcs)
                        {
                            _log.LogDebug(string.Format("R-CRC {0}  Before:{1}  After:{2}  L:{3} {4}", c.Offset.ToString(l9 ? "X9" : "X8"), c.Value.ToString("X8"), c.PatchCrc == 0 ? "        " : c.PatchCrc.ToString("X8"), c.Length.ToString(l9 ? "X9" : "X8"), SourceFiles.CleanseFileName(c.Name)));
                        }

                        _log.LogDebug(string.Format("ReadCRC {0}Before:{1} After:{2}", l9 ? " " : "", pc.ReaderCrcs.FullCrc(false).ToString("X8"), pc.ReaderCrcs.FullCrc(true).ToString("X8")));
                    }
                    if (pc.WriterCrcs != null)
                    {
                        foreach (CrcItem c in pc.WriterCrcs.Crcs)
                        {
                            _log.LogDebug(string.Format("W-CRC {0}  Before:{1}  After:{2}  L:{3} {4}", c.Offset.ToString(l9 ? "X9" : "X8"), c.Value.ToString("X8"), c.PatchCrc == 0 ? "        " : c.PatchCrc.ToString("X8"), c.Length.ToString(l9 ? "X9" : "X8"), SourceFiles.CleanseFileName(c.Name)));
                        }

                        _log.LogDebug(string.Format("WriteCRC {0}Before:{1} After:{2}", l9 ? " " : "", pc.WriterCrcs.FullCrc(false).ToString("X8"), pc.WriterCrcs.FullCrc(true).ToString("X8")));
                    }
                    _log.ProcessingComplete(results.OutputSize, results.ProcessorMessage, true);
                };

                try
                {
                    _log.ProcessingStart(input.SourceSize, Title);

                    using (fs = new StreamCircularBuffer(size, input, null, s => Reader.Read(ctx, input, s, pc))) //read in stream and write to circular buffer
                    {
                        Writer.Write(ctx, fs, output, pc);
                    }
                }
                catch
                {
                    if (pc.Exception != null)
                    {
                        throw pc.Exception;
                    }

                    if (fs.WriterException != null)
                    {
                        throw fs.WriterException;
                    }

                    throw; //writer exception
                }

                foreach (CrcItem crc in pc.Patches.Crcs.Where(a => a.PatchData != null || a.PatchFile != null))
                {
                    output.Seek(crc.Offset, SeekOrigin.Begin);
                    if (crc.PatchFile == null)
                    {
                        output.Write(crc.PatchData, 0, (int)Math.Min(crc.Length, crc.PatchData.Length)); //PatchData might be larger
                    }
                    else
                    {
                        using (FileStream pf = File.OpenRead(crc.PatchFile))
                        {
                            pf.Copy(output, pf.Length);
                            ByteStream.Zeros.Copy(output, crc.Length - pf.Length);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_timer != null)
                {
                    _timer.Enabled = false;
                }

                try
                {
                    _log.ProcessingComplete(results.OutputSize, results.ProcessorMessage, false); // force any log lines to be output - handy for diagnosis
                }
                catch { }
                throw new HandledException(ex, "Failed processing {0} -> {1}", Reader?.GetType()?.Name ?? "<null>", Writer?.GetType()?.Name ?? "<null>");
            }
            finally
            {
                if (_timer != null)
                {
                    _timer.Enabled = false;
                    _timer = null;
                }
            }

            return results;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1} [{2}/{3}] => {4} [{5}/{6}])", GetType().Name, Reader.GetType().Name, Reader.RequireValidationCrc ? "Vld" : "-", Reader.RequireVerifyCrc ? ("Vfy" + (Reader.VerifyIsWrite ? "-W" : "-R")) : "-", Writer.GetType().Name, Writer.RequireValidationCrc ? "Vld" : "-", Writer.RequireVerifyCrc ? ("Vfy" + (Writer.VerifyIsWrite ? "-W" : "-R")) : "-");
        }

    }
}
