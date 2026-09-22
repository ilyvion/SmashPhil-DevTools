using System;
using System.Globalization;
using System.IO;
using JetBrains.Annotations;

namespace DevTools.Testing;

/// <summary>
/// Writes test log messages to a text file.
/// </summary>
[UsedImplicitly]
internal sealed class TextLogWriter : ILogWriter
{
  private readonly FileStream fileStream;
  private readonly StreamWriter writer;

  public TextLogWriter(Logger logger)
  {
    FileMode mode = FileMode.Append;
    if (logger.IsOwner)
    {
      // Creates or clears log file, we can immediately close it
      // since we want to open with StreamWriter with append mode.
      mode = FileMode.Create;
    }
    fileStream = new FileStream(logger.LogConfig.FullPath, mode, FileAccess.Write, FileShare.ReadWrite);
    writer = new StreamWriter(fileStream) { AutoFlush = true };
  }

  void IDisposable.Dispose()
  {
    fileStream.Dispose();
  }

  void ILogWriter.PostInit()
  {
    SeekToEnd();
    writer.WriteLine($"{DateTime.Now.ToString("g", DateTimeFormatInfo.CurrentInfo)}");
    writer.WriteLine("");
  }

  void ILogWriter.Flush()
  {
    writer.Flush();
  }

  void ILogWriter.WriteLine(string message)
  {
    // FileMode.Append only seeks to end-of-file once, at construction; it does not track writes made by
    // other processes (parent and child both keep a long-lived stream open on the same Test.log across
    // a test plan run), so a stream idle while another process appends must reseek before writing or it
    // overwrites what the other process wrote in the meantime.
    SeekToEnd();
    writer.WriteLine(message);
  }

  private void SeekToEnd()
  {
    fileStream.Seek(0, SeekOrigin.End);
  }
}
