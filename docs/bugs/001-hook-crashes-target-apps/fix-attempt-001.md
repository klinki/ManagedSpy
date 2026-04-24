# Fix Attempt 001

## Attempt Status
awaiting-user-confirmation

## Goal
Stop ManagedSpy from crashing external managed applications by preventing hook injection into incompatible runtimes.

## Relation To Previous Attempts
First attempt for this bug.

## Proposed Change
- Tighten `Desktop::IsManagedProcess` compatibility checks:
  - Detect .NET Framework runtime modules and treat them as incompatible.
  - Detect `coreclr.dll` and require runtime major version 10 or newer.
  - Mark incompatible processes as non-managed for hook purposes.
- Keep compatible .NET 10+ processes eligible for existing inspection flow.

## Risks
- False negatives may hide some compatible processes if runtime detection is too strict.
- Module/version probing may fail for some protected processes.

## Files And Components
- `ManagedSpyLib\Commands.cpp`
- `docs\bugs\001-hook-crashes-target-apps\status.md`

## Verification Plan
- Build solution for `Release|x64` and `Release|x86`.
- Confirm no compile errors and no regression in hook pipeline build output.

## Implementation Summary
- Updated `Desktop::IsManagedProcess` in `ManagedSpyLib\Commands.cpp` to gate hook eligibility by runtime compatibility, not only by "managed" presence.
- Added module classification helpers:
  - .NET Framework runtime modules (`mscorlib.dll`, `mscorlib.ni.dll`, `clr.dll`) are treated as incompatible.
  - Modern .NET runtime modules (`coreclr.dll`, `System.Private.CoreLib.dll`, `System.Runtime.dll`) are considered compatible only when file major version is `>= 10`.
- Changed return behavior to `isManaged && isCompatibleRuntime`, so incompatible managed processes are excluded from hook injection.

## Test Results
- `MSBuild.exe ManagedSpy.sln /t:Build /p:Configuration=Release /p:Platform=x64` ✅
- `MSBuild.exe ManagedSpy.sln /t:Build /p:Configuration=Release /p:Platform=x86` ✅
- Existing C++/CLI warning `C4642` remains (pre-existing after migration), no new build errors introduced.

## Outcome
Local fix is implemented and compiled successfully. ManagedSpy should now skip incompatible managed runtimes instead of injecting the .NET 10 hook assembly into them.

## Next Step
User validation: confirm that starting ManagedSpy no longer crashes non-.NET-10 target applications.

## Remaining Gaps
- Needs confirmation in user environment with previously crashing applications.
