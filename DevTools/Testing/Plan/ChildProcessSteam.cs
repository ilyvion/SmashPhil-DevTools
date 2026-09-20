using System;
using HarmonyLib;
using Verse.Steam;

namespace DevTools.Testing;

internal static class ChildProcessSteam
{
  // Steam drops the client connection of a game started by a running game, and any later Steam call in that
  // process kills it with a pipe assert. A test plan's child process doesn't need Steam, so it's marked
  // uninitialized, which makes the game skip its Steam calls.
  public static void DisableInChildProcess()
  {
    if (Environment.GetEnvironmentVariable(TestProcess.ChildVariable) == null)
      return;

    AccessTools.Field(typeof(SteamManager), "initializedInt").SetValue(null, false);
  }
}
