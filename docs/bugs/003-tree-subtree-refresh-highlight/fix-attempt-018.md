# Fix Attempt 018

## Attempt Status
implemented-diagnostics-complete

## Goal
Capture enough runtime evidence from the user's environment to identify which highlight-bounds source remains stale on repeated target switches.

## Relation To Previous Attempts
Follow-up to `fix-attempt-017.md`, which still showed no change. At this point the remaining bug is too specific to the user's environment and app mix to keep guessing safely without concrete runtime data.

## Proposed Change
- Add diagnostic logging for persistent-highlight updates.
- Record, on each target switch or rectangle change:
  - selected proxy identity
  - previous highlighted rectangle
  - preferred rectangle
  - non-accessibility retry rectangle
  - raw-window fallback rectangle
  - chosen rectangle and source

## Risks
- Diagnostic logging adds file I/O while reproducing the issue.
- The resulting log is intentionally temporary and investigative, not a final user-facing feature.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the existing helper coverage in x64.
- User reproduces the bug and shares the generated diagnostic log.

## Implementation Summary
- Added `ManagedSpy-highlight-diagnostics.log` output next to the executable.
- Logged persistent highlight switch/update data from `MainForm` whenever the highlighted target changes or the chosen rectangle changes.
- Included the rectangle candidates and final chosen source in each log line.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt18` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt18\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 4/4 tests.

## Outcome
The next user reproduction can now tell us exactly which rectangle source is stale, instead of forcing another blind fix attempt.

## Follow-up Update
- 2026-04-27: user shared a diagnostic log excerpt.
- The captured bad switch showed the preferred rectangle source being chosen first and later moving to a different position only after the target app was clicked.
- Superseded by `fix-attempt-019.md`, which uses that evidence to prefer the non-accessibility path on target switches when it can immediately replace the old highlighted rectangle.

## Next Step
User reproduces the repeated highlight-switch issue and shares the generated `ManagedSpy-highlight-diagnostics.log`.

## Remaining Gaps
- The bug is still open pending runtime evidence from the user's environment.
