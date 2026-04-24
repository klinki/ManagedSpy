# Fix Attempt 003

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Ensure persistent highlight keeps tracking the target while drag/drop or move operations are changing control handles.

## Relation To Previous Attempts
Follow-up to `fix-attempt-002.md`, which corrected z-order behavior but still tracked persistent highlight by a stored handle value.

## Proposed Change
- Track persistent highlight by `ControlProxy` reference instead of a frozen `IntPtr` handle.
- Resolve the current handle from the proxy on every highlight timer tick.
- Keep z-order logic from attempt 002.

## Risks
- If proxy synchronization fails for edge cases, highlight could still temporarily lag.
- Handle updates depend on existing proxy event propagation (`WM_HANDLECHANGED`) staying intact.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Startup smoke run of the produced x64 binary.

## Implementation Summary
- Replaced `persistentHighlightHandle` field with `persistentHighlightProxy`.
- Updated enable/disable and timer-update logic to read `persistentHighlightProxy.Handle` each tick.
- Updated context-menu check and toggle logic to compare against the current proxy handle.
- Preserved existing non-topmost z-order strategy from attempt 002.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out` succeeded for Release x86/x64.
- Startup smoke run from x64 build output stayed alive during initial window scan.

## Outcome
Persistent highlight tracking is now resilient to target handle changes, reducing stale-position behavior during drag/drop flows.

## Next Step
User confirmation in the reported drag-and-drop scenario.

## Remaining Gaps
- Behavioral confirmation in the user environment is still pending.
