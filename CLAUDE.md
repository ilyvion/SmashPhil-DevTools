# DevTools (fork)

Fork of SmashPhil/DevTools (remote `phil`/`upstream`, branch `develop`); `origin` is ilyvion/SmashPhil-DevTools.

## Build and test launch setup

The build and launch tooling lives outside this repository, in `/home/alex/Documents/Projects/RimWorld/rimworld-utils`:

- `build.sh` and `gabs-launch.sh`: symlinked into each mod project's `.vscode/`. `gabs-launch.sh` runs `build.sh`, then launches RimWorld (Flatpak Steam) under GABS.
- `devtools.md`: reference for using DevTools from a mod (attributes, CLI args, `Test.log` format, gotchas, GABS test workflow). Keep it in sync when changing DevTools' CLI, attributes or output.
- `gabs.md`: GABS workflow rules, including how to wait for and read a test run's result.

GABS game definitions are in `~/.gabs/config.json`. Each mod's game id is its project name, with launch inputs `runTests` (`--test --exit`), `testFilter` (`--where <expr>`), `testPlan` (`--test-plan <name> --exit`) and `rimworldVersion`.

DevTools is not installed as a mod of its own. Each mod with tests references `DevTools/DevTools.csproj` from its test project, which builds `DevTools.dll` into that mod's `<version>/Assemblies`. To try a change made here, build and launch a mod that uses it. `build.sh` deletes every `DevTools.dll` in the RimWorld `Mods` folder before installing a mod, because the game loads every copy in active mods and a stale copy from another mod can handle the command line instead.

Results go to `<mod project>/.savedatafolder/<version>/Test.log`, next to `Player.log`.

## Testing DevTools itself with RealisticOrbitalTrade

`/home/alex/Documents/Projects/RimWorld/RealisticOrbitalTrade` (GABS game id `RealisticOrbitalTrade`, RimWorld 1.7) is the best mod for checking DevTools changes:

- 82 fixtures and about 500 tests, from unit tests to in-game end-to-end tests, across two test assemblies (`Source/RealisticOrbitalTrade.Tests`, `Source/RealisticOrbitalTrade.SocialEXPFromTrade.Tests`).
- Test plans in `TestPlans/` (`Biotech`, `BulkLoad`, `Smoke`, `SocialExp`).
- A full run takes about 30 seconds of test time; `testFilter: "class == BuyMoreTimeTests"` is a quick filtered run (3 tests).

`DroppedCargoProtectionEndToEndTests.ADownedHaulerDropsItsInventoryProtected` and `QuestPartTests.TransferPartGivesTheTraderWhatIsAboardAndRemembersSoldPrisoners` have failed in a full run while passing when run on their own; treat a failure of only those two as a mod-side test issue, not a DevTools regression.

## Build status

The RimWorld 1.7 build succeeds. The 1.6 build fails: `TcpBridge.cs` needs `System.Text.Json` and `TestMessage.cs` uses `init` accessors, neither of which the `net48` target has.
