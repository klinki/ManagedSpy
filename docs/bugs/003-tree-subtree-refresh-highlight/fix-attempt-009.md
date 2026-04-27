# Fix Attempt 009

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Stabilize the target-process screen-bounds calculation used by `Keep Highlighted` and add automated coverage for nested-control geometry.

## Relation To Previous Attempts
Follow-up to `fix-attempt-008.md`, which added a target-process screen-bounds query but still produced invalid/off-screen geometry in user testing.

## Proposed Change
- Extract screen-bounds logic into a reusable helper in `ManagedSpyLib`.
- Switch the helper formula to use:
  - top-level `Bounds` for controls without parents
  - `RectangleToScreen(ClientRectangle)` for child controls
  - a point/size fallback only if client bounds are empty
- Add automated tests around the helper with real WinForms forms/panels/labels.

## Risks
- Child-control client-rectangle highlighting may exclude nonclient borders for some controls.
- Automated tests validate the helper formula in-process, but the full cross-process highlight flow still needs manual retest.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `ManagedSpyLib\Commands.cpp`
- `ManagedSpyLib\ControlProxy.h`
- `ManagedSpyLib\ControlProxy.cpp`
- `ManagedSpyLib\MessageFilters.cpp`
- `ManagedSpyLib\Messages.h`
- `ManagedSpyLib\ManagedSpyLib.vcxproj`
- `ManagedSpyLib\ScreenBoundsHelper.h`
- `ManagedSpyLib\ScreenBoundsHelper.cpp`
- `ManagedSpy.Tests\ManagedSpy.Tests.csproj`
- `ManagedSpy.Tests\ScreenBoundsHelperTests.cs`
- `ManagedSpy.sln`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the new helper in x64.
- Startup smoke run of the produced x64 binary.

## Implementation Summary
- Added `ScreenBoundsHelper::GetControlScreenBounds(Control^)` in `ManagedSpyLib`.
- Updated `WM_GETMGDSCREENRECT` handling to call the shared helper instead of inlining ad hoc geometry math.
- Kept `ControlProxy.GetScreenBounds()` and `MainForm` persistent-highlight fallback behavior.
- Added a new `ManagedSpy.Tests` MSTest project with two WinForms-based tests:
  - top-level form bounds
  - nested label client-screen bounds
- Added the test project to the solution so it builds with the repo.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt9` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt9\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 2/2 tests.
- Startup smoke run from x64 build output stayed alive during initial window scan.

## Outcome
Persistent highlight now uses a test-backed screen-bounds helper in the target process instead of an unverified inline formula.

## Follow-up Update
- 2026-04-27: user reported the highlight was still absolutely off and shared a screenshot where the persistent rectangle extends far beyond the visible selected item.
- Superseded by `fix-attempt-010.md`, which clips the selected control's screen bounds to ancestor client rectangles.

## Next Step
User confirmation on the deep-tree and off-screen highlight scenarios.

## Remaining Gaps
- Behavioral confirmation in the user environment is still pending.
