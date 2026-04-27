# Fix Attempt 008

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Make `Keep Highlighted` use the selected managed control's own screen bounds so deep tree components highlight themselves instead of an ancestor/native window.

## Relation To Previous Attempts
Follow-up to `fix-attempt-007.md`, which fixed context-menu targeting but still relied on geometry resolved from the proxy handle.

## Proposed Change
- Add a new cross-process message to retrieve the selected control's screen bounds directly from the spied process.
- Use the target control's managed parent/Bounds relationship there, with raw HWND rectangle fallback in the inspector.
- Preserve the context-menu targeting fix from attempt 007.

## Risks
- Cross-process screen-bounds query must remain serializable and safe for controls whose parent is null.
- Some controls may still require fallback to raw HWND rectangles if screen bounds cannot be resolved.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `ManagedSpyLib\Messages.h`
- `ManagedSpyLib\MessageFilters.cpp`
- `ManagedSpyLib\Commands.cpp`
- `ManagedSpyLib\ControlProxy.h`
- `ManagedSpyLib\ControlProxy.cpp`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Startup smoke run of the produced x64 binary.

## Implementation Summary
- Added `WM_GETMGDSCREENRECT` message and allowed it through the message filter.
- Added `ControlProxy.GetScreenBounds()` to query target-process screen bounds from the selected control.
- Implemented `WM_GETMGDSCREENRECT` handling in `Commands.cpp`:
  - if the control has a parent, convert the managed `Bounds` location through the parent with `PointToScreen`
  - otherwise use top-level `Bounds`
- Updated `MainForm` persistent highlight to prefer `proxy.GetScreenBounds()` and only fall back to `GetWindowRect` when needed.
- Preserved the explicit tree context-menu target behavior from attempt 007.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt8` succeeded for Release x86/x64.
- Startup smoke run from x64 build output stayed alive during initial window scan.

## Outcome
Persistent highlight now uses target-process managed screen bounds when available, which should better match the selected tree component than raw proxy-handle geometry.

## Next Step
User confirmation on deep tree components such as labels that previously highlighted an ancestor container.

## Remaining Gaps
- Behavioral confirmation in the user environment is still pending.
