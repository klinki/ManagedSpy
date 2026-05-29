# Bug Description

## Title
Find element on screen is slow to select the matching tree item

## Status
- fixed

## Reported Symptoms
- After the Refresh performance fix, **Find element on screen** still takes a long time to select the correct item in the tree view.
- This selection path used to be faster.
- Attempt 001 made selection faster, but user retesting reported a `NullReferenceException` crash in `treeWindow_BeforeExpand`.
- Attempt 002 fixed the expansion crash locally, but user retesting reported a follow-up `NullReferenceException` in `treeWindow_AfterSelect`.
- Attempt 003 fixed the selection crash locally, but user retesting reported incomplete tree results with missing sub-components.
- Attempt 004 improved lazy tree loading, but user reported **Find element on screen** still does not find/show the correct element in the tree.
- Attempt 005 aligned refresh-created top-level nodes with finder-created lazy nodes, but user retesting still reported the finder as broken.
- Attempt 006 improved finder behavior under some conditions, but user retesting identified the core remaining tree regression: applications with multiple top-level forms can show only one form.
- Attempt 007 fixed the tree regression, but user retesting reported that finder selection still freezes the UI.
- Attempt 008 fixed finder responsiveness and selection correctness according to user confirmation.

## Expected Behavior
- Clicking a target with **Find element on screen** should quickly select the corresponding tree node.
- Selection should avoid a full window refresh or broad child enumeration when the target handle is already known.

## Actual Behavior
- The finder path can still perform expensive tree traversal/expansion work before selecting the node.
- Programmatic tree expansion can trigger the same "one step ahead" child enumeration used for manual browsing.
- A background refresh can still be running when the finder click is processed, allowing refresh completion to rebuild the tree after finder selection.
- A correct selection can be hard to see if ManagedSpy does not return focus to the tree after the user clicks the target app.
- Refresh and finder top-level population used each individual HWND's managed status as an inclusion gate, so a compatible managed process could lose top-level forms whose specific HWND did not answer as managed at that moment.
- Finder path selection still performed cross-process proxy and child queries on the UI thread.
- Tree selection also fetched Layout data even when the Layout tab was not active.
- Finder selection ended with a blocking flash animation implemented with `Thread.Sleep` on the UI thread.

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
- Whether any remaining failures after attempt 006 are wrong-HWND selection, wrong tree path selection, or selection visibility.
- Whether including all top-level windows for compatible managed processes fully restores the multi-form tree behavior in the user's target application.
- Whether background finder snapshot creation fully resolves the remaining perceived UI freeze in the user's target application.
