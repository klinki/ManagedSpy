# Bug Description

## Title
Starting ManagedSpy crashes target .NET applications due to hook runtime mismatch

## Status
- awaiting-user-confirmation (attempt 002)

## Reported Symptoms
- After starting ManagedSpy, other managed applications crash.
- The target process reports:
  - `System.IO.FileNotFoundException`
  - Missing assembly: `System.Runtime, Version=10.0.0.0`
  - Failure in `AssemblyLoadContext.LoadFromInMemoryModule`.
- After initial fix, ManagedSpy itself can crash at startup with:
  - `System.ComponentModel.Win32Exception (5): Access denied`
  - Source path: `Desktop::IsManagedProcess` while reading `Process.Modules`.

## Expected Behavior
ManagedSpy should inspect compatible target applications or safely skip incompatible ones without crashing external processes.

## Actual Behavior
ManagedSpy previously crashed external target apps due runtime mismatch. After that mitigation, some environments still crash ManagedSpy itself during process scan when module enumeration is denied for certain processes.

## Reproduction Details
1. Start a non-.NET-10 managed desktop application.
2. Start ManagedSpy (migrated to .NET 10).
3. ManagedSpy enumerates windows and attempts hook injection.
4. Target app crashes with missing `System.Runtime, Version=10.0.0.0`.

## Affected Area
- `ManagedSpyLib\Commands.cpp` (`Desktop::IsManagedProcess`, hook eligibility)
- Hook injection path (`Desktop::EnableHook` via `SetWindowsHookEx`)

## Constraints
- Must prevent crashes in external applications.
- Must preserve inspection behavior for compatible targets.
- Keep existing x86/x64 behavior.

## Open Questions
- Whether compatibility should be limited to .NET 10 exactly or .NET 10+ runtimes.
- Whether additional telemetry is needed for skipped inaccessible processes.
