# Bug Description

## Title
Find element on screen is slow to select the matching tree item

## Status
- open

## Reported Symptoms
- After the Refresh performance fix, **Find element on screen** still takes a long time to select the correct item in the tree view.
- This selection path used to be faster.
- Attempt 001 made selection faster, but user retesting reported a `NullReferenceException` crash in `treeWindow_BeforeExpand`.
- Attempt 002 fixed the expansion crash locally, but user retesting reported a follow-up `NullReferenceException` in `treeWindow_AfterSelect`.
- Attempt 003 fixed the selection crash locally, but user retesting reported incomplete tree results with missing sub-components.

## Expected Behavior
- Clicking a target with **Find element on screen** should quickly select the corresponding tree node.
- Selection should avoid a full window refresh or broad child enumeration when the target handle is already known.

## Actual Behavior
- The finder path can still perform expensive tree traversal/expansion work before selecting the node.
- Programmatic tree expansion can trigger the same "one step ahead" child enumeration used for manual browsing.

## Reproduction Details
1. Launch ManagedSpy.
2. Use **Find element on screen**.
3. Click a managed target control.
4. Observe that selecting the matching tree item takes a long time.

## Affected Area
- `ManagedSpy\MainForm.cs`
  - element finder click handling
  - tree path selection
  - `treeWindow_BeforeExpand`

## Constraints
- Keep the tree behavior for manual expansion.
- Preserve the background Refresh fix.
- Do not touch WinForms controls from a background thread.

## Open Questions
- Whether there are target-specific trees with very broad/deep control hierarchies that still need additional optimization after path-only selection.
