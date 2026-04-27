# Fix Attempt 020

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Force the highlight overlay window to repaint immediately after moving to a new rectangle.

## Relation To Previous Attempts
Follow-up to `fix-attempt-019.md`. The latest diagnostic log showed that the chosen rectangle was already changing immediately on target switches, which means the remaining lag is likely in the overlay repaint path rather than in the rectangle-selection logic itself.

## Proposed Change
- After moving the overlay with `SetWindowPos`, call `Update()` so the form paints synchronously on the same UI turn.

## Risks
- Synchronous repainting is slightly more eager than the prior invalidate-only path.
- This affects both the persistent highlight overlay and the finder overlay because they share the same form class.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the existing helper coverage in x64.
- User retest on repeated highlight switching between different nodes.

## Implementation Summary
- Updated `HighlightOverlayForm.ShowHighlight` to call `Update()` immediately after `Invalidate()`.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt20` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt20\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 4/4 tests.

## Outcome
The overlay form now repaints synchronously after each move, so a stale on-screen red frame should not have to wait for another user input event to catch up.

## Next Step
User confirmation on whether repeated highlight switches now repaint immediately.

## Remaining Gaps
- Behavioral confirmation in the user's application set is still pending.
