# Fix Attempt 013

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Resolve `Keep Highlighted` against the originally selected managed control instead of re-identifying that control by HWND alone during later highlight updates.

## Relation To Previous Attempts
Follow-up to `fix-attempt-012.md`, which still showed no user-visible change. Additional diagnostics showed that:
- `Refresh Subtree` did not help.
- `Show Window` on the same tree node did not visibly highlight the target either.

That points to the selected tree node's HWND identity being insufficient for some custom controls, rather than the bug living only inside one specific bounds formula.

## Proposed Change
- Serialize the selected control's managed parent/child path into `ControlProxy`.
- Send that path along with `WM_GETMGDSCREENRECT`.
- In the target process, resolve the control again from the managed path before computing highlight bounds.

## Risks
- Child-index paths can become stale if a container dynamically reorders its `Controls` collection after the proxy was captured.
- Property-grid access still remains handle-based for now; this attempt only changes the highlight path.

## Files And Components
- `ManagedSpyLib\ControlProxy.h`
- `ManagedSpyLib\ControlProxy.cpp`
- `ManagedSpyLib\Commands.cpp`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the existing helper coverage in x64.
- User retest on the same tree-node scenario where `Show Window` also failed to visibly highlight the target.

## Implementation Summary
- Added a serialized `managedChildPath` field to `ControlProxy`.
- Captured the control's managed child-index path when the proxy is created in the target process.
- Updated `ControlProxy.GetScreenBounds()` to send that path with `WM_GETMGDSCREENRECT`.
- Updated `Commands.cpp` to resolve the control from the managed root and child path before computing screen bounds.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt13` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt13\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 4/4 tests.

## Outcome
Persistent highlight no longer depends on `Control::FromHandle(msg->hwnd)` returning the right managed control at update time; it now re-resolves the control from the managed tree path captured when the node proxy was created.

## Next Step
User confirmation on the custom wizard-option scenario where both `Keep Highlighted` and `Show Window` previously failed to point at the visible target.

## Remaining Gaps
- Behavioral confirmation in the user's application is still pending.
