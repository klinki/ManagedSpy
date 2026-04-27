# Fix Attempt 016

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Force the target application to repaint immediately when switching the persistent highlight to a different component, so the new component highlights without waiting for a manual click in the target window.

## Relation To Previous Attempts
Follow-up to `fix-attempt-015.md`. User feedback after that attempt showed:
- The first highlighted selection worked.
- Switching to another selected node left the previous component highlighted until the user clicked in the target app window.

That indicates the right target is now mostly identified correctly, but the target app is not repainting soon enough for the new bounds source to update on its own.

## Proposed Change
- Hide the existing persistent overlay immediately when the target proxy changes.
- Request a redraw of the target root window before computing the new persistent highlight rectangle.

## Risks
- For some target apps, forcing a redraw could be more expensive than passive polling.
- The redraw request must stay limited to target switches, not every timer tick.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the existing helper coverage in x64.
- User retest on repeated node-to-node `Keep Highlighted` switching in eM Client and/or ManagedSpy.

## Implementation Summary
- Added `RedrawWindow` P/Invoke and redraw flags in `MainForm`.
- Updated `EnablePersistentHighlight` to detect when the highlighted proxy changes.
- When switching targets, hide the current overlay, request a redraw of the target root window, then fetch and show the new highlight.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt16` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt16\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 4/4 tests.

## Outcome
Persistent highlight target switches now actively request the target app to repaint instead of waiting for user interaction to make the new component bounds visible.

## Follow-up Update
- 2026-04-27: user reported no visible change after this attempt.
- The redraw request alone was not enough; switching to a new highlighted node still required a click in the target app before the new component appeared.
- Superseded by `fix-attempt-017.md`, which treats a repeated old rectangle on target switch as stale accessibility data and retries without the accessibility path.

## Next Step
User confirmation on repeated `Keep Highlighted` selection changes.

## Remaining Gaps
- Behavioral confirmation in the user's application set is still pending.
