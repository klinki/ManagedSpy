# Fix Attempt 014

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Normalize cross-process highlight rectangles into physical screen coordinates when a target app appears to report managed/accessibility bounds in a DPI-scaled logical coordinate space.

## Relation To Previous Attempts
Follow-up to `fix-attempt-013.md`. User feedback after that attempt showed:
- ManagedSpy itself could sometimes hit the right component.
- eM Client remained completely off.
- The user explicitly suspected DPI.

That app-dependent difference strongly suggests a DPI-awareness mismatch between the inspector and at least some target applications.

## Proposed Change
- Detect when a managed/accessibility-derived rectangle does not even fit inside the real top-level window in physical screen coordinates.
- In that case, convert the rectangle through `LogicalToPhysicalPointForPerMonitorDPI`.
- Keep the original rectangle when it already fits the real top-level window so ordinary scenarios do not regress.

## Risks
- Mixed DPI-awareness combinations across applications can still be tricky, so the conversion must stay heuristic rather than unconditional.
- This attempt only normalizes managed/accessibility-derived rectangles; native `GetWindowRect`-based paths already use physical coordinates.

## Files And Components
- `ManagedSpyLib\ScreenBoundsHelper.cpp`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the existing helper coverage in x64.
- User retest in both ManagedSpy itself and eM Client if possible.

## Implementation Summary
- Added top-level-root detection for the selected managed control.
- Added a managed-rectangle normalization step that can convert screen points from logical to physical coordinates via `LogicalToPhysicalPointForPerMonitorDPI`.
- Restricted that conversion so it only wins when the original rectangle does not fit inside the real top-level window and the converted rectangle does.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt14b` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt14b\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 4/4 tests.

## Outcome
Persistent highlight now has a DPI-normalization path for managed/accessibility-derived rectangles when the target app appears to be reporting them in a logical coordinate space rather than physical screen pixels.

## Next Step
User confirmation on whether eM Client still shows the large offset after this DPI-focused normalization.

## Remaining Gaps
- Behavioral confirmation in the user's DPI-mixed application set is still pending.
