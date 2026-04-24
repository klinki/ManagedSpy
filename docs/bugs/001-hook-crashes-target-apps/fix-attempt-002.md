# Fix Attempt 002

## Attempt Status
awaiting-user-confirmation

## Goal
Prevent ManagedSpy itself from crashing when inspecting processes whose module list cannot be read due to access restrictions.

## Relation To Previous Attempts
Supersedes `fix-attempt-001.md`. Attempt 001 fixed target-app runtime mismatch, but user reported a new crash in ManagedSpy startup caused by `Process.Modules` access denial.

## Proposed Change
- In `Desktop::IsManagedProcess`, handle process/module inspection failures explicitly:
  - `System.ComponentModel.Win32Exception`
  - `System.InvalidOperationException`
  - `System.ArgumentException`
  - `System.NotSupportedException`
- For these cases, mark process as non-eligible and continue scanning other windows instead of throwing.
- Keep runtime compatibility guard from attempt 001 intact.

## Risks
- More processes may be skipped, reducing discoverability.
- Overly broad failure handling could hide diagnostics if not constrained to known inspection exceptions.

## Files And Components
- `ManagedSpyLib\Commands.cpp`
- `docs\bugs\001-hook-crashes-target-apps\status.md`

## Verification Plan
- Build Release x64/x86.
- Start ManagedSpy and confirm it stays running through initial window scan.

## Implementation Summary
- Updated `Desktop::IsManagedProcess` in `ManagedSpyLib\Commands.cpp` to explicitly guard process/module inspection with targeted exception handling.
- Added catches for:
  - `System.ComponentModel.Win32Exception`
  - `System.ArgumentException`
  - `System.InvalidOperationException`
  - `System.NotSupportedException`
- On these failures, ManagedSpy now safely classifies the process as non-eligible and continues scanning, instead of propagating the exception and crashing.
- Kept runtime compatibility gating from attempt 001 unchanged.

## Test Results
- `MSBuild.exe ManagedSpy.sln /t:Build /p:Configuration=Release /p:Platform=x64` ✅
- `MSBuild.exe ManagedSpy.sln /t:Build /p:Configuration=Release /p:Platform=x86` ✅
- Startup smoke test: launched `ManagedSpy.exe` and kept it running through initial scan window (`SpyAliveAfterStartupScan=True`) ✅
- Regression smoke test: launched temporary `net8.0-windows` WinForms victim + ManagedSpy together; both stayed alive during observation window ✅

## Outcome
ManagedSpy no longer crashes when encountering inaccessible processes during startup scan; inaccessible process/module inspection paths are now handled safely.

## Next Step
User validation in the original environment that triggered `Win32Exception (5)` during startup.

## Remaining Gaps
- User-side validation against original environment remains required after local verification.
