# Fix Attempt 017

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Avoid reusing a stale accessibility rectangle when switching the persistent highlight to a different selected component.

## Relation To Previous Attempts
Follow-up to `fix-attempt-016.md`, which forced a target redraw on highlight switches but still did not change the user's observed lag. The remaining symptom is very specific:
- first selection works
- following selections still show the previous highlight until the target app is clicked

That pattern suggests the accessibility-based bounds source may be lagging one selection behind on target switches.

## Proposed Change
- On the first update after switching targets, if the newly queried rectangle is identical to the previously highlighted rectangle, treat it as stale.
- Immediately retry that one query without the accessibility-bounds path and use the next-best managed/native geometry instead.

## Risks
- Some legitimate cases could reuse the same rectangle across two different controls, though that is unlikely in the user's reported scenario.
- This fallback only applies on target switches; the normal accessibility-first path remains unchanged once the highlight settles.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `ManagedSpyLib\ControlProxy.h`
- `ManagedSpyLib\ControlProxy.cpp`
- `ManagedSpyLib\Commands.cpp`
- `ManagedSpyLib\ScreenBoundsHelper.h`
- `ManagedSpyLib\ScreenBoundsHelper.cpp`
- `ManagedSpy.Tests\ScreenBoundsHelperTests.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the helper coverage in x64.
- User retest on repeated highlight switching between different tree nodes.

## Implementation Summary
- Added a `preferAccessibility` option to `ScreenBoundsHelper::GetControlScreenBounds` and to `ControlProxy.GetScreenBounds`.
- Extended the `WM_GETMGDSCREENRECT` parameters so the target process can resolve both the managed control path and whether accessibility should be preferred for that query.
- On target switches, `MainForm` now compares the first returned rectangle against the previously highlighted rectangle and retries without accessibility if they are identical.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt17` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt17\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 4/4 tests.

## Outcome
Persistent highlight target switches no longer have to trust an accessibility rectangle that looks exactly like the old highlighted region; they can immediately fall back to non-accessibility geometry for that one transition.

## Follow-up Update
- 2026-04-27: user reported no change after this attempt.
- Superseded by `fix-attempt-018.md`, which adds diagnostic logging so the next reproduction captures the actual candidate rectangles chosen in the user's environment.

## Next Step
User confirmation on repeated switching between different highlighted nodes.

## Remaining Gaps
- Behavioral confirmation in the user's application set is still pending.
