# Bug Description

## Title
Tree subtree does not always include dynamically added children; add subtree refresh and persistent highlight controls

## Status
- awaiting-user-confirmation

## Reported Symptoms
- Some nested controls are missing in the component tree (example: children of `MailClient.Common.UI.Controls.TableLayoutPanelEx`).
- A full **Refresh Windows** repopulates the missing nodes, indicating stale tree data.
- Persistent highlight currently draws above unrelated windows that are on top of the target control.

## Expected Behavior
- The selected subtree can be refreshed directly from the tree without rebuilding the entire window/process list.
- A selected component can be kept highlighted on screen until explicitly toggled off.
- Persistent highlight should stay in the target window stack and not render above other windows that cover the target.

## Actual Behavior
- Tree updates rely on global refresh for dynamic child visibility in some flows.
- Persistent highlight initially used a topmost overlay, which could appear above unrelated windows.

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
