# Bug Description

## Title
Keep Highlighted position is incorrect for eM Client child controls

## Status
- open

## Reported Symptoms
- `Keep Highlighted` draws the persistent red overlay in the wrong location for selected eM Client controls.
- In `screenshots\keep_highlighted_invalid_location.png`, the selected `optionButton_CustomSetup...` tree item should highlight the option row, but the overlay is a large rectangle lower on the window.
- In `screenshots\highlight_login_button.png`, the selected `button_EmSync_Login` tree item should highlight the visible **Log in** button, but the overlay appears near the bottom-right of the Settings window.
- The **Find elements on the screen** magnifier path works correctly for the same target area according to the user.

## Expected Behavior
- `Keep Highlighted` should draw around the same visible target that the tree selection represents.
- Persistent highlighting should use the same coordinate space as the overlay window.
- Persistent highlighting should log enough geometry and DPI context to diagnose bad choices in user environments.

## Actual Behavior
- Persistent highlight uses target-process managed/accessibility bounds and local raw HWND rectangles through multiple heuristics.
- For eM Client, some returned rectangles appear to be in a different DPI/coordinate space than the overlay.
- Recent fallback attempts still allow the persistent overlay to land far from the selected child control.

## Reproduction Details
1. Start eM Client and ManagedSpy.
2. Inspect eM Client from ManagedSpy.
3. Select a nested eM Client control such as `button_EmSync_Login` or `optionButton_CustomSetup...` in the tree.
4. Use the tree context menu **Keep Highlighted**.
5. Observe that the persistent red overlay is not drawn around the selected visible control.

## Affected Area
- `ManagedSpy\MainForm.cs`
  - `TryGetPersistentHighlightRectangle`
  - persistent-highlight diagnostics
  - local coordinate normalization and raw-window fallback
- `ManagedSpyLib\ControlProxy.cs`
  - `managedChildPath`
  - `GetScreenBounds`
- `ManagedSpyLib\Desktop.cs`
  - cross-process control path resolution
- `ManagedSpyLib\ScreenBoundsHelper.cs`
  - target-process screen-bounds calculation
  - raw-window DPI fallback helper

## Constraints
- Preserve the working magnifier/finder behavior.
- Avoid replacing child-control rectangles with ancestor/root HWND rectangles.
- Avoid overfitting to one screenshot without logging enough data to prove the coordinate source.
- Keep the bug open until the user confirms the fix in eM Client.

## Open Questions
- Is the selected control's `proxy.Handle` the child control's own HWND, or an ancestor HWND?
- Are `proxy.GetScreenBounds()` results physical target-process coordinates while `GetWindowRect(proxy.Handle)` is virtualized for ManagedSpy?
- Does `PhysicalToLogicalPointForPerMonitorDPI(this.Handle, ...)` change anything when ManagedSpy is DPI-unaware?
- Which source matches the magnifier path for the same selected tree node?
