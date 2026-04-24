# Initial Findings

## Confirmed Facts
- Tree expansion uses a one-level-ahead population strategy (`treeWindow_BeforeExpand`).
- In that strategy, descendants can become stale until a broader refresh path repopulates the branch.
- Tree context menu currently supports only transient highlighting (`Show Window`/flash).

## Likely Cause
- Dynamic controls may be introduced after a branch is initially populated, and the existing flow does not provide a targeted subtree rebuild action from the selected node.

## Unknowns
- How large subtree refresh operations perform in very large UI trees.
- Whether users also need subtree refresh for process root nodes in addition to component nodes.

## Reproduction Status
- User-reported issue is consistent with the current lazy tree population flow.
- Code inspection confirms no dedicated subtree refresh command in the tree context menu.

## Evidence Gathered
- `MainForm.cs` tree handling (`RefreshWindows`, `treeWindow_BeforeExpand`, context-menu handlers).
- Existing context menu structure from `MainForm.Designer.cs`.
