using System;
using System.IO;
using System.Runtime.InteropServices;

namespace DevTools.Testing;

internal static class ChildProcessFileDescriptors
{
  private const int F_GETFD = 1;
  private const int F_SETFD = 2;
  private const int FD_CLOEXEC = 1;

  [DllImport("libc", SetLastError = true)]
  private static extern int fcntl(int fd, int cmd, int arg);

  // On Linux, Process.Start() forks this process, so any fd without O_CLOEXEC is duplicated into the
  // child. Steam's native client keeps its IPC sockets open that way, so a spawned child inherits live
  // copies of the parent's Steam connection and its own steamclient.so collides with them, crashing with
  // a cross-process pipe assert. Marking every inherited fd close-on-exec right before spawning keeps
  // them from surviving the child's exec.
  public static void PreventInheritance()
  {
    if (Environment.OSVersion.Platform != PlatformID.Unix)
      return;

    const string fdDir = "/proc/self/fd";
    if (!Directory.Exists(fdDir))
      return;

    foreach (string path in Directory.GetFiles(fdDir))
    {
      if (!int.TryParse(Path.GetFileName(path), out int fd) || fd <= 2)
        continue;

      int flags = fcntl(fd, F_GETFD, 0);
      if (flags == -1 || (flags & FD_CLOEXEC) != 0)
        continue;

      fcntl(fd, F_SETFD, flags | FD_CLOEXEC);
    }
  }
}
