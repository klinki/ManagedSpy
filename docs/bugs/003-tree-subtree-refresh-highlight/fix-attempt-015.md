# Fix Attempt 015

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Apply tree context-menu actions only after the menu closes so the new target highlight is computed and painted on a clean UI cycle.

## Relation To Previous Attempts
Follow-up to `fix-attempt-014.md`. User feedback after that attempt was the strongest positive signal so far:
- Both eM Client and ManagedSpy were now almost correct.
- The remaining issue was timing: enabling `Keep Highlighted` from the context menu left the previous component highlighted until the user clicked the target app, at which point the correct component was redrawn.

That symptom points at menu/UI timing rather than another geometry mismatch.

## Proposed Change
- Defer tree context-menu actions with `BeginInvoke` so they run after the context menu closes.
- Apply the same deferral to:
  - `Keep Highlighted`
  - `Show Window`
  - `Refresh Subtree`

## Risks
- Deferred execution means the action happens one UI turn later by design.
- Captured tree nodes must still be valid when the deferred action runs.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the existing helper coverage in x64.
- User retest of the context-menu `Keep Highlighted` flow in both eM Client and ManagedSpy if possible.

## Implementation Summary
- Added `RunAfterTreeMenuClose(Action)` in `MainForm`.
- Refactored subtree refresh so it can target a captured node directly.
- Deferred `Refresh Subtree`, `Show Window`, and `Keep Highlighted` so they execute after the context menu finishes closing.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt15` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt15\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 4/4 tests.

## Outcome
Tree context-menu highlight actions no longer run while the menu is still active, which should let the newly selected node replace the previous highlight immediately instead of waiting for another click to trigger the redraw.

## Next Step
User confirmation on whether `Keep Highlighted` now switches to the correct component immediately in eM Client and ManagedSpy.

## Remaining Gaps
- Behavioral confirmation in the user's application set is still pending.
