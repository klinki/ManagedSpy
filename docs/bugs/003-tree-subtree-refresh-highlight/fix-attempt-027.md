# Fix Attempt 027

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Fall back to the local raw Win32 rectangle when the managed target rectangle is just a near-uniform DPI-scaled twin of it.

## Relation To Previous Attempts
Follow-up to `fix-attempt-026.md`. The local conversion attempt did not change the logs at all, but the logs still show a very strong signal: the bad managed rectangle keeps matching the local raw rectangle multiplied by the same DPI factor.

## Proposed Change
- In `ManagedSpy\MainForm.cs`, compare the managed target rectangle against the local raw rectangle already collected from `GetWindowRect`.
- When the managed rectangle is a near-uniform scale-up of the raw rectangle and the raw rectangle fits the local root window better, choose the local raw rectangle instead.
- Keep this heuristic conservative so it only triggers on the repeated DPI-scale signature, not on ordinary child-versus-ancestor differences.

## Risks
- If a valid child-control highlight genuinely differs from the raw handle rectangle without being a simple DPI-scale mismatch, the fallback must not steal that case.
- This still relies on user retesting because the affected DPI-aware target app is not available locally.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64` into an isolated output root.
- Run the existing `ManagedSpy.Tests.dll` tests from the built output.
- User retest in eM Client against the same option-button and button scenarios from the latest logs.

## Implementation Summary
- Added a conservative local fallback in `MainForm` that compares the managed target rectangle against the local raw Win32 rectangle.
- Detects the repeated DPI-mismatch signature when the managed rectangle is a near-uniform scale-up of the raw rectangle and the raw rectangle fits the local root window better.
- Chooses the raw rectangle in those cases and records a dedicated diagnostic source so the next log can confirm the fallback triggered.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\5361b3fb-b28f-428a-8c27-aa25cfafd720\files\build-out-attempt27` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\5361b3fb-b28f-428a-8c27-aa25cfafd720\files\build-out-attempt27\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 5/5 tests.
- `dotnet vstest C:\Users\david\.copilot\session-state\5361b3fb-b28f-428a-8c27-aa25cfafd720\files\build-out-attempt27\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 5/5 tests.

## Outcome
- ManagedSpy now has a narrow local fallback for the repeated DPI-scale mismatch pattern seen in the eM Client diagnostics, allowing the already-correct local raw rectangle to win only in those cases.

## Follow-up Update
- 2026-04-28: user confirmed this attempt fixed the issue in eM Client.
- The reported persistent highlight offset is gone, and the user asked for automated regression coverage to keep this DPI-mismatch case from coming back.
- Extracted the raw-window DPI fallback heuristic into `ManagedSpyLib.ScreenBoundsHelper` and added four deterministic tests covering the real positive and negative eM Client ratio cases. The regression test suite now exercises 9 total cases.

## Next Step
- User retest in eM Client against the same option-button and button scenarios from the latest logs.

## Remaining Gaps
- None for the confirmed fix path; regression coverage is now in place for the raw-window DPI-scale fallback.
