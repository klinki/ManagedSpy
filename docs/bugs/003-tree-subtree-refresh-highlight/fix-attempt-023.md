# Fix Attempt 023

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Destroy and recreate the persistent highlight overlay window on target switches so stale visuals cannot remain tied to a reused transparent form instance.

## Relation To Previous Attempts
Follow-up to `fix-attempt-022.md`. The remaining behavior still looks like a stale frame surviving a switch even though the correct rectangle is already known. This attempt treats the overlay window instance itself as suspect.

## Proposed Change
- When switching to a different highlighted target:
  - dispose the existing persistent highlight overlay form
  - create a fresh non-topmost overlay form
  - show the new target highlight with the new overlay instance

## Risks
- Recreating the overlay window on each target switch is heavier than reusing one instance.
- This still focuses on the rendering path rather than the geometry path.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the existing helper coverage in x64.
- User retest on repeated highlight switching between different nodes.

## Implementation Summary
- Changed `persistentHighlightOverlay` from readonly to replaceable.
- Added `ResetPersistentHighlightOverlay()` to dispose the existing overlay and create a fresh one.
- Reset the persistent highlight overlay whenever the highlighted proxy changes.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt23` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt23\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 4/4 tests.

## Outcome
Persistent highlight target switches now use a fresh overlay window instance instead of trying to reuse a possibly stale transparent form.

## Next Step
User confirmation on whether the stale old frame finally disappears immediately on switches.

## Remaining Gaps
- Behavioral confirmation in the user's application set is still pending.
