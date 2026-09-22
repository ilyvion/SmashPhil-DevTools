using System.Collections.Generic;
using JetBrains.Annotations;

namespace DevTools.Testing;

[PublicAPI]
public struct TestPlanJob
{
  public string name;
  public string commandLineArgs;
  public List<string> loadWithMods;
  public float timeOut;

  // Assigned by TestPlanManager from the job's position in TestPlan.jobs, not read from XML.
  // Lets TestProcess give each child a unique log file instead of clobbering its siblings'.
  public int childIndex;
}