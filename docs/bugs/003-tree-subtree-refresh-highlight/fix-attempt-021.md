# Fix Attempt 021

## Attempt Status
implemented-awaiting-user-diagnostics

## Goal
Expand the diagnostic log so the next reproduction reveals whether the raw HWND geometry is already stable while the managed rectangle drifts, or whether both paths move together after the target app click.

## Relation To Previous Attempts
Follow-up to `fix-attempt-020.md`, which still showed no visible change. The latest user log proved that the chosen rectangle can still move after the click, but it did not include the raw target HWND rectangle or the root-window rectangle, so we still cannot tell whether the drift starts in managed geometry or in the raw Win32 window coordinates themselves.

## Proposed Change
- Always log:
  - raw target window rect
  - root-window rect
  - alongside the existing preferred / non-accessibility / chosen rectangles

## Risks
- This is diagnostic-only and does not attempt another blind fix yet.
- The bug remains open until the new evidence is captured.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the existing helper coverage in x64.
- User reproduces the issue again and shares the expanded log.

## Implementation Summary
- Added `root` rectangle logging.
- Changed diagnostics collection to capture raw target-window and root-window rectangles on every logged update, not only on fallback.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt21` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt21\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 4/4 tests.

## Outcome
The next log capture will finally show whether the post-click drift originates in the raw window coordinates or only in the managed rectangle sources.

## Follow-up Update
- 2026-04-27: user shared the expanded diagnostic log.
- The new evidence showed raw/root/chosen rectangles already stable at switch time, yet the visible highlight still lagged until the target app was clicked.
- Superseded by `fix-attempt-022.md`, which targets stale old-frame cleanup after the overlay move rather than more rectangle-source changes.

## Next Step
User reproduces the issue again and shares the expanded `ManagedSpy-highlight-diagnostics.log`.

## Remaining Gaps
- The bug is still open pending the new diagnostic evidence.
