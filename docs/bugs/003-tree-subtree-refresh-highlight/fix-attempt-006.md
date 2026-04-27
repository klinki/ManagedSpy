# Fix Attempt 006

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Correct the persistent highlight geometry regression so `Keep Highlighted` uses the same on-screen rectangle source as the working magnifier highlight.

## Relation To Previous Attempts
Follow-up to `fix-attempt-005.md`, which attempted to fix post-drop lag by preferring managed bounds, but user feedback and screenshot evidence show that path can produce incorrect geometry.

## Proposed Change
- Remove the managed-bounds geometry path introduced in attempt 005.
- Use the actual HWND rectangle again for persistent highlight, matching the magnifier implementation.
- Increase persistent highlight polling cadence to match the magnifier timer for more responsive updates.

## Risks
- This may re-expose some drag/drop lag scenarios if the underlying issue was not geometry-source related.
- Faster polling increases update frequency slightly, though it remains a lightweight rectangle query.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Startup smoke run of the produced x64 binary.

## Implementation Summary
- Removed the `ClientToScreen` P/Invoke and the managed-bounds helper methods added by attempt 005.
- Updated persistent highlight refresh to use `TryGetWindowRectangle(windowHandle, out rectangle)` directly again.
- Changed persistent highlight timer interval from 150 ms to 80 ms to match the magnifier path.
- Preserved the non-topmost z-order behavior from attempt 002 and the proxy/handle propagation hardening from attempts 003-004.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out` succeeded for Release x86/x64.
- Startup smoke run from x64 build output stayed alive during initial window scan.

## Outcome
Persistent highlight geometry is now aligned with the proven magnifier implementation instead of the regressed managed-bounds path.

## Next Step
User confirmation in the application shown in the screenshot and in the original drag/drop scenario.

## Remaining Gaps
- Behavioral confirmation in the user environment is still pending.
