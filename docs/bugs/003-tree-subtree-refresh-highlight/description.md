# Bug Description

## Title
Tree subtree does not always include dynamically added children; add subtree refresh and persistent highlight controls

## Status
- open

## Reported Symptoms
- Some nested controls are missing in the component tree (example: children of `MailClient.Common.UI.Controls.TableLayoutPanelEx`).
- A full **Refresh Windows** repopulates the missing nodes, indicating stale tree data.
- Persistent highlight currently draws above unrelated windows that are on top of the target control.
- During drag-and-drop/move scenarios, persistent highlight can remain at an old position instead of following the component.
- In some drag/drop cases, highlight jumps to correct position only after the user clicks the target window again.
- Current `Keep Highlighted` geometry can be wildly incorrect in some applications even when the magnifier highlight is correct.
- `Keep Highlighted` can target the last component selected by the magnifier instead of the tree node that opened the context menu.
- In eM Client, the persistent highlight can again land noticeably below the selected label even though the selected tree node is correct (see `screenshots\bug_dpi_issues.png`).
- In eM Client, `Keep Highlighted` can again draw a large red rectangle below the selected `optionButton_CustomSetup` item (see `screenshots\keep_highlighted_invalid_location.png`).
- In eM Client, `Keep Highlighted` can draw near the lower-right of the Settings window instead of on the selected `button_EmSync_Login` item (see `screenshots\highlight_login_button.png`).

## Expected Behavior
- The selected subtree can be refreshed directly from the tree without rebuilding the entire window/process list.
- A selected component can be kept highlighted on screen until explicitly toggled off.
- Persistent highlight should stay in the target window stack and not render above other windows that cover the target.
- Persistent highlight should continue tracking the component position while drag/drop operations are in progress.
- No post-drop click should be required to resynchronize highlight position.
- Persistent highlight should use the same reliable on-screen geometry as the magnifier highlight.
- Tree context-menu actions should always operate on the exact node the user right-clicked.

## Actual Behavior
- Tree updates rely on global refresh for dynamic child visibility in some flows.
- Persistent highlight initially used a topmost overlay, which could appear above unrelated windows.
- Persistent highlight was keyed to a stored handle value; if the target handle changed during interactive operations, highlight tracking could lag or stay stale.
- Handle-change propagation could miss updates in cache edge-cases, leaving highlight attached to stale handle state until a later interaction.
- The managed-bounds-based path from attempt 005 can produce incorrect rectangles for some custom/mixed controls even when raw HWND-based magnifier highlighting is accurate.
- Context-menu actions were still resolving through `treeWindow.SelectedNode`, allowing magnifier-driven selection state to override the node that actually opened the menu.
- Even with explicit menu targeting, resolving geometry purely from the proxy handle can still highlight an ancestor/native window instead of the selected managed control's true screen bounds.
- The first target-process screen-bounds formula from attempt 008 could still return invalid/off-screen geometry for some controls, so the screen-bounds computation itself needed to be isolated and tested.
- The current first-selection path still prefers accessibility-derived bounds whenever they are non-empty, even if they may no longer line up with the selected control's native client geometry in eM Client.
- New diagnostics show that for some eM Client controls, both the preferred and non-accessibility persistent-highlight rectangles can be over-scaled by the same 1.75x DPI factor, pointing to the shared normalization step rather than accessibility alone.
- ManagedSpy currently has no explicit DPI-awareness manifest/config, so a DPI-aware target app can still return screen rectangles that do not match the local coordinate space used by ManagedSpy's overlay window.
- Even after a local `PhysicalToLogicalPointForPerMonitorDPI` pass in ManagedSpy, the logged rectangles can remain unchanged, so the fallback may need to rely on the already-correct local raw window rectangle instead of API-based conversion alone.

## Reproduction Details
1. Start ManagedSpy and inspect a UI with dynamically added controls.
2. Expand into nested controls.
3. Observe missing descendants under some nodes.
4. Trigger **Refresh Windows** and observe the descendants appear.

## Affected Area
- `ManagedSpy\MainForm.cs`
  - Tree context menu behavior.
  - Subtree rebuild logic for control nodes.
  - Overlay highlight behavior for tree-selected controls.

## Constraints
- Keep existing tree browsing and finder behavior intact.
- Keep highlight rendering artifact-free (overlay approach).

## Open Questions
- Whether a subtree refresh should eventually be exposed for process nodes as well (current request is component-node focused).
- Whether any non-accessibility/native screen-bounds source still needs DPI normalization in a target app after the shared over-scaling is removed.
- Whether local overlay-space normalization in `MainForm` is sufficient, or if ManagedSpy itself ultimately needs an explicit DPI-awareness declaration.
- Whether the repeated eM Client cases can be fixed safely by falling back to the local raw Win32 rectangle only when the managed rectangle is a near-uniform DPI-scaled version of it.
- Whether the new research-first investigation in `docs\bugs\005-keep-hignlighted-position\` identifies the persistent highlight coordinate mismatch root cause.
