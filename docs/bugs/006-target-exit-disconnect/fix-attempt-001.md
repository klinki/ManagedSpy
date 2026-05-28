# Fix Attempt 001

## Attempt Status
fixed

## Goal
Disconnect cleanly from inspected target processes when they exit and avoid locking target application files while reading metadata.

## Relation To Previous Attempts
First attempt for this bug.

## Proposed Change
- Replace `Assembly.LoadFile(assemblyPath)` target metadata loading with a byte-array load so the source file is not held open by ManagedSpy.
- Add process-exit tracking for process nodes in the ManagedSpy tree.
- On target process exit:
  - stop event logging if the selected/current proxy belongs to the target
  - disable persistent highlight if it belongs to the target
  - remove the target process node from the tree
  - clear stale selected/property state
  - remove cached proxies for that process
  - dispose the tracked `Process` object

## Risks
- Byte-loaded assemblies may not behave exactly like path-loaded assemblies for dependency/resource resolution.
- Removing a process node on exit changes the tree immediately, but stale exited targets should not remain actionable.
- Cached proxy cleanup must use stable captured process ownership, not live HWND process lookup after the target windows have been destroyed.

## Files And Components
- `ManagedSpyLib\ControlProxy.cs`
- `ManagedSpyLib\Desktop.cs`
- `ManagedSpy\MainForm.cs`
- `docs\bugs\006-target-exit-disconnect\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64` into an isolated output root.
- Run `ManagedSpy.Tests.dll` from both built outputs with matching `/Platform` values.
- Ask the user to retest with the target app: inspect, exit target, then verify target files can be modified while ManagedSpy remains open.

## Implementation Summary
- Replaced `Assembly.LoadFile(assemblyPath)` with `Assembly.Load(File.ReadAllBytes(assemblyPath))` through `LoadAssemblyWithoutLock` so ManagedSpy does not hold target assembly files open just to resolve metadata.
- Added stable `ControlProxy` process ownership captured when proxies are created, including marshaled managed proxies and HWND-only proxies.
- Added process disconnect cleanup through `ControlProxy.DisconnectProcess` and `Desktop.RemoveCachedProxiesForProcess`.
- Removed process IDs from the managed/unmanaged classification caches and removed cached proxies using the captured owner process ID.
- Added `MainForm` process tracking with `EnableRaisingEvents`, UI-thread process-exit handling, process-node removal, selected-property cleanup, event logging stop, persistent-highlight disablement, and `Process` disposal.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\disconnect-attempt1-final-build` passed.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\disconnect-attempt1-final-build\x64\ManagedSpy.Tests.dll /Platform:x64 --logger:"console;verbosity=minimal"` passed: 14/14.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\disconnect-attempt1-final-build\x86\ManagedSpy.Tests.dll /Platform:x86 --logger:"console;verbosity=minimal"` passed: 14/14.

## Outcome
- Local implementation and automated validation completed successfully.
- Bug remains open until the user confirms ManagedSpy disconnects after target exit and target files can be modified while ManagedSpy stays open.
- 2026-05-28: user confirmed the fix works.

## Next Step
- None.

## Remaining Gaps
- Byte-loaded target assemblies should be watched during retest for any metadata browsing regressions.
