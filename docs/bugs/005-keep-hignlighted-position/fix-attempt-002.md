# Fix Attempt 002

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Prevent `Keep Highlighted` from continuing to use stale eM Client Settings-dialog proxies after the dialog is closed and reopened.

## Relation To Previous Attempts
Follow-up to diagnostic attempt 001. The user found that highlighting works on the first Settings dialog instance, then breaks after closing and reopening Settings. This points to stale proxy/handle/path lifetime rather than a pure DPI heuristic.

## Proposed Change
- Publish target `WindowDestroyed` and `HandleChanged` notifications from `ControlProxy`/`EventTargetWindow`.
- Subscribe `MainForm` to those notifications.
- When the persistent-highlight target handle is destroyed, disable persistent highlighting and hide the overlay instead of keeping a stale proxy alive.
- Remove stale tree nodes whose `ControlProxy.Handle` was destroyed so old Settings-dialog controls cannot be selected after the dialog closes.
- Keep handle-change notifications updating existing tree nodes for legitimate WinForms handle recreation.

## Risks
- Removing destroyed tree nodes can change the visible tree while the target app is closing controls, but this is preferable to keeping invalid selections.
- If a target app destroys and recreates a handle transiently for the same control without `RecreatingHandle`, the persistent highlight will turn off and require re-enabling.

## Files And Components
- `ManagedSpyLib\ControlProxy.cs`
- `ManagedSpyLib\EventTargetWindow.cs`
- `ManagedSpy\MainForm.cs`
- `docs\bugs\005-keep-hignlighted-position\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64` into an isolated output root.
- Run `ManagedSpy.Tests.dll` from both built outputs with matching `/Platform` values.
- Ask the user to retest: open eM Client Settings, enable `Keep Highlighted`, close Settings, reopen Settings, refresh/reselect if needed, and verify stale highlights do not persist or jump to wrong positions.

## Implementation Summary
- Added static `ControlProxy.WindowDestroyed` and `ControlProxy.HandleChanged` notifications raised by `EventTargetWindow` when target-process controls report destroyed or recreated handles.
- Subscribed `MainForm` to those lifecycle notifications.
- When the persistent-highlight target handle is destroyed, `MainForm` now disables persistent highlight, hides the overlay, and avoids continuing timer updates against the stale proxy.
- Destroyed proxy handles are removed from the visible tree so closed Settings-dialog controls cannot remain selectable after the dialog closes.
- Legitimate handle-change notifications update matching tree node proxy handles and node keys.
- Existing diagnostic logging from attempt 001 remains in place.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\lifecycle-attempt2-build` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\lifecycle-attempt2-build\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 14/14 tests.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\lifecycle-attempt2-build\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 14/14 tests.
- Focused code review found no significant issues in the lifecycle changes.

## Outcome
- Persistent highlighting no longer keeps using a destroyed target handle after the Settings dialog closes.
- Stale tree nodes for destroyed target handles are removed, reducing the chance of selecting an old Settings-dialog proxy after reopening the dialog.

## Next Step
- Ask the user to retest the first-open/close/reopen Settings flow in eM Client.

## Remaining Gaps
- Direct reproduction in eM Client is still unavailable in this environment.
- If the user wants `Keep Highlighted` to automatically reacquire a semantically equivalent control after the dialog reopens, that would require a separate control identity/reselection feature.
