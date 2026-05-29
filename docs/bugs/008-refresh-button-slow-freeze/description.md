# Bug Description

## Title
Refresh button is slow and freezes the UI

## Status
- fixed

## Reported Symptoms
- Clicking **Refresh** takes much longer than before.
- The ManagedSpy UI freezes while refresh is running.
- The slowdown appears to be a regression from recent lifecycle/file-lock/feature work.

## Expected Behavior
- Refresh should keep the UI responsive.
- Expensive window discovery and managed-process probing should not run on the UI thread.
- Refresh should avoid unnecessary target assembly/type loading.

## Actual Behavior
- `RefreshWindows` runs synchronously on the UI thread.
- The refresh path evaluates `ControlProxy.IsManaged` for top-level windows, which can resolve `ComponentType`.
- `ComponentType` now loads target assemblies from byte arrays to avoid file locks, so invoking it during refresh can significantly slow discovery.
- Attempt 001 moved the expensive discovery work off the UI thread and avoided the `ComponentType` path during normal managed filtering.
- User confirmed the attempt 001 result as fixed/much better on 2026-05-29.

## Reproduction Details
1. Launch ManagedSpy.
2. Click the toolbar **Refresh** button or **View > Refresh**.
3. Observe that refresh takes a long time and the UI is unresponsive until it finishes.

## Affected Area
- `ManagedSpy\MainForm.cs`
  - `RefreshWindows`
  - refresh toolbar/menu handlers
  - process tracking
- `ManagedSpyLib\ControlProxy.cs`
  - `IsManaged`
  - top-level window enumeration API
- `ManagedSpyLib\Desktop.cs`
  - top-level window discovery
  - proxy cache access

## Constraints
- Keep target-exit disconnect behavior.
- Keep target assembly loading no-lock behavior.
- Do not touch WinForms controls from a background thread.
- Preserve current tree structure and process-exit tracking after refresh completes.

## Open Questions
- Whether the background refresh is sufficiently responsive with large numbers of top-level windows in the user's environment.
- Whether additional cancellation should be added if the user wants refresh to be interruptible.
