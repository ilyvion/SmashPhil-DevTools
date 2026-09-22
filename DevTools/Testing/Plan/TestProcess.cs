using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using Verse;

namespace DevTools.Testing;

// Sole test fixture for test plans. It kicks off a process which will load and run the plan's tests
// separately, while this fixture waits for the process to exit.
internal sealed class TestProcess
{
  // Set in the child's environment so it can tell it was started by a test plan.
  internal const string ChildVariable = "DEVTOOLS_TEST_CHILD";

  private const int DefaultTimeOut = 600000; // 10 minutes

  // Parent launch arguments that the child must share to use the same save/config/log locations.
  // The log file is suffixed with the job's child index so siblings from the same plan don't
  // overwrite each other's log.
  private const string LogFileArg = "-logfile";
  private static readonly string[] InheritedArgs = ["-savedatafolder", LogFileArg];

  // Passed to the child so it can identify itself among its siblings, e.g. in its own logging.
  private const string ChildIndexArg = "--child-index";

  private Process process;
  private bool timedOut;

  public ModContentPack mod;
  public TestPlanJob job;

  [OneTimeSetUp]
  private void SpawnProcess()
  {
    if (process != null)
      return;

    if (!job.loadWithMods.NullOrEmpty())
    {
      if (!job.loadWithMods.Contains(mod.PackageId) &&
          !job.loadWithMods.Contains(mod.ModMetaData.PackageIdNonUnique.ToLowerInvariant()))
      {
        Test.Fail($"Loading mod list without plan runner {mod.Name} loaded. This is not supported.");
        return;
      }
      ModsConfig.SaveFromList(job.loadWithMods);
    }
    string fileName = Environment.GetCommandLineArgs()[0];
    StringBuilder claBuilder = new();
    claBuilder.Append($"--pid \"{mod.PackageId}\" -e {ChildIndexArg} {job.childIndex}"); // -batchmode
    foreach (string arg in InheritedArgs)
    {
      if (ContainsArg(job.commandLineArgs, arg) || GetParentArgValue(arg) is not { } value)
        continue;

      if (arg == LogFileArg)
      {
        value = Path.Combine(Path.GetDirectoryName(value) ?? "",
          $"{Path.GetFileNameWithoutExtension(value)}-child{job.childIndex}{Path.GetExtension(value)}");
      }
      // The game only recognizes -savedatafolder in the -name=value form; -logfile takes a separate value.
      claBuilder.Append(arg == LogFileArg ? $" {arg} \"{value}\"" : $" {arg}=\"{value}\"");
    }
    if (!job.commandLineArgs.NullOrEmpty())
    {
      claBuilder.Append($" {job.commandLineArgs}");
    }

    process = new Process();
    process.StartInfo.FileName = fileName;
    process.StartInfo.Arguments = claBuilder.ToString();
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.EnvironmentVariables[ChildVariable] = "1";
    process.StartInfo.CreateNoWindow = false;
    process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
    Assert.IsTrue(process.Start(), "Process was unable to be started.");
  }

  [OneTimeTearDown]
  private void DisposeProcess()
  {
    if (process is { HasExited: false })
    {
      // If process has to be killed on disposal, it's considered a failure. It should've properly terminated
      // at the end of its own test runner.
      Test.Fail("Killing process before it was able to finish.");
      Kill();
    }
    process?.Dispose();
    process = null;
  }

  [Test, HideInUI]
  private IEnumerator WaitForProcess()
  {
    const float ProcessPollInterval = 0.5f;

    float startTime = Time.realtimeSinceStartup;
    float timeOut = job.timeOut > 0 ? job.timeOut : DefaultTimeOut;

    while (!process.HasExited)
    {
      if (Time.realtimeSinceStartup >= startTime + timeOut)
      {
        timedOut = true;
        break;
      }
      yield return new WaitForSecondsRealtime(ProcessPollInterval);
    }
    if (timedOut || !process.HasExited)
    {
      Test.Fail("Timed Out...");
      Kill();
    }
    int exitCode = process.ExitCode;
    DevLog.Write($"Finished with exit code {exitCode}");
    Expect.AreEqual(expected: 0, exitCode, $"Process exited with code {exitCode}");
  }

  private static bool ContainsArg(string args, string name)
  {
    return !args.NullOrEmpty() &&
           args.Split(' ').Any(a => IsArg(a, name));
  }

  private static string GetParentArgValue(string name)
  {
    string[] args = Environment.GetCommandLineArgs();
    for (int i = 1; i < args.Length; i++)
    {
      if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        return i + 1 < args.Length ? args[i + 1] : null;
      if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
        return args[i].Substring(name.Length + 1);
    }
    return null;
  }

  private static bool IsArg(string arg, string name)
  {
    return string.Equals(arg, name, StringComparison.OrdinalIgnoreCase) ||
           arg.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase);
  }

  private void Kill()
  {
    if (process is null || process.HasExited)
      return;

    const int KillTimeOutMs = 2000; // ms

    try
    {
      process.Kill();
      if (!process.WaitForExit(KillTimeOutMs))
      {
        Test.Fail("Failed to kill process, it may be orphaned.");
      }
    }
    catch (Exception ex)
    {
      Test.Fail(ex);
    }
  }
}
