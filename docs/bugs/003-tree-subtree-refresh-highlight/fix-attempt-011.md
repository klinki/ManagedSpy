# Fix Attempt 011

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Move `Keep Highlighted` child-control positioning back onto native HWND screen coordinates so the rectangle aligns with the selected tree item instead of being translated away from it.

## Relation To Previous Attempts
Follow-up to `fix-attempt-010.md`, which fixed oversized rectangles by clipping to ancestor client bounds, but user feedback with a second screenshot showed the rectangle was still offset even when its shape was closer to the target.

## Proposed Change
- Stop using WinForms `RectangleToScreen` / `PointToScreen` as the primary coordinate source for child controls.
- Resolve the selected control's client rectangle through native HWND APIs (`GetClientRect` + `MapWindowPoints`) and clip that against ancestor HWND client rectangles.
- Keep top-level controls on a raw window-rectangle path.

## Risks
- This assumes the selected managed control still has a meaningful HWND-backed client area.
- Controls with unusual nonclient rendering may still need a later fallback adjustment.

## Files And Components
- `ManagedSpyLib\ScreenBoundsHelper.cpp`
- `ManagedSpy.Tests\ScreenBoundsHelperTests.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the helper in x64.
- User retest on the previously offset `Keep Highlighted` scenario.

## Implementation Summary
- Added native window-bounds and client-bounds helpers in `ScreenBoundsHelper.cpp`.
- Changed child-control screen-bounds resolution to use `GetClientRect` + `MapWindowPoints` on the selected control's HWND.
- Changed ancestor clipping to use native HWND client rectangles first, with the previous WinForms path only as fallback.
- Updated automated tests to assert against native user32-derived expected rectangles instead of WinForms screen-coordinate helpers.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt11` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt11\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 3/3 tests.

## Outcome
Persistent highlight now derives managed child-control rectangles from the same native HWND coordinate space used by the working magnifier path, while still clipping to the selected control's visible ancestor client area.

## Follow-up Update
- 2026-04-27: user reported this attempt produced the same incorrect result as before.
- Superseded by `fix-attempt-012.md`, which prefers custom accessibility bounds for child controls.

## Next Step
User confirmation on the still-offset deep-tree highlight scenario from screenshot 02.

## Remaining Gaps
- Behavioral confirmation in the user's application is still pending.
