# Fix Attempt 010

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Make `Keep Highlighted` draw the selected control's visible on-screen region instead of the full unclipped control rectangle.

## Relation To Previous Attempts
Follow-up to `fix-attempt-009.md`, which stabilized the target-process bounds helper and added tests, but still treated a control's full client rectangle as visible even when ancestor containers clipped it.

## Proposed Change
- Update `ScreenBoundsHelper` to intersect the selected control's screen bounds with each ancestor's client rectangle.
- Keep top-level controls on the existing full-window path.
- Add automated coverage for an oversized child control that is partially clipped by its parent.

## Risks
- Some owner-drawn controls may still visually render a smaller region than their clipped client rectangle.
- Intersecting against ancestor client rectangles intentionally excludes nonclient areas for nested controls.

## Files And Components
- `ManagedSpyLib\ScreenBoundsHelper.cpp`
- `ManagedSpy.Tests\ScreenBoundsHelperTests.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the helper in x64.
- Re-check the user screenshot against the new clipping logic.

## Implementation Summary
- Updated `ScreenBoundsHelper::GetControlScreenBounds(Control^)` to clip child control bounds against every ancestor client rectangle.
- Kept the fallback path for zero-sized `RectangleToScreen(ClientRectangle)` results.
- Added a third WinForms-based test that verifies an oversized child control is clipped to its parent's visible client area.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt10` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt10\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 3/3 tests.

## Outcome
Persistent highlight now clips target-process child-control bounds to the visible region allowed by their parent containers, which matches the oversized off-screen failure pattern shown in the user's screenshot.

## Follow-up Update
- 2026-04-27: user reported the rectangle shape was closer to the selected component, but it was still offset significantly and shared a second screenshot showing the entire highlight translated away from the target.
- Superseded by `fix-attempt-011.md`, which moves child-control coordinate mapping onto native HWND screen coordinates.

## Next Step
User confirmation on the previously off-screen / oversized highlight scenario.

## Remaining Gaps
- Behavioral confirmation in the user's target application is still pending.
