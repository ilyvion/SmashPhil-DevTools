using HarmonyLib;
using UnityEngine;
using Verse;

namespace DevTools;

internal static class RunInBackground
{
  // A test run's window must keep updating while unfocused, e.g. while a test plan's child window has focus,
  // so the game's "run in background" preference is overridden for the whole run.
  public static void Enable()
  {
    Application.runInBackground = true;
    DevHarmony.Harmony.Patch(
      original: AccessTools.Method(typeof(PrefsData), nameof(PrefsData.Apply)),
      postfix: new HarmonyMethod(typeof(RunInBackground), nameof(KeepEnabled)));
  }

  private static void KeepEnabled()
  {
    Application.runInBackground = true;
  }
}
