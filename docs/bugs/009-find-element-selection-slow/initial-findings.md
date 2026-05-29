# Initial Findings

## Confirmed Facts
- The element finder obtains the clicked target handle directly.
- The previous selection path looked for an existing tree node and could fall back to refresh-style discovery.
- `treeWindow_BeforeExpand` preloads children for every child under the expanded node to keep expand glyphs ready for manual browsing.
- Programmatic `Expand`/`EnsureVisible` during finder selection can therefore enumerate far more controls than needed for a single known target path.

## Likely Cause
- Finder selection is paying the cost of broad tree population instead of directly materializing the ancestor path for the clicked control.

## Unknowns
- Exact performance impact depends on the target application's control hierarchy size.

## Reproduction Status
- Not reproduced interactively in this environment; implementation is based on the reported regression and code-path inspection.

## Evidence Gathered
- `ManagedSpy\MainForm.cs`
  - `elementFinderTimer_Tick`
  - `FocusWindowInTree`
  - `treeWindow_BeforeExpand`

## 2026-05-29 Follow-up Findings
- User confirmation after attempt 005 reported that **Find element on screen** is still broken while the Layout behavior is acceptable.
- `MainForm_Load` starts `RefreshWindowsAsync`, and `ApplyRefreshSnapshot` clears and rebuilds `treeWindow` when the background snapshot completes.
- `elementFinderTimer_Tick` can process a finder click while that async refresh is still in progress, so a correct finder selection can be erased by the pending refresh completion.
- Finder selection was set before ancestor expansion and without returning focus to ManagedSpy/treeWindow, making a successful selection less visible after the user clicks into the target application.

## 2026-05-29 Multi-form Tree Regression Findings
- User retesting after attempt 006 confirmed the finder is better under some conditions, but an application with two main forms now shows only one form in the tree.
- Reviewing the diff since `47c0a5754d32a5cb2f1117c17b33a39986132d92` found that both `BuildRefreshSnapshot` and `PopulateProcessTopLevelWindows` still filtered each top-level HWND independently when **Show Native Windows** is off.
- That per-HWND filter can drop a real top-level form from a compatible managed process if the specific HWND does not return `IsKnownManagedProxy`/`IsManaged` during enumeration.
- The refresh tree is rebuilt only from the filtered snapshot, so any skipped top-level HWND is unrecoverable until a later refresh happens to include it.
- `Desktop.GetProxy` also reused cached proxies without checking whether the HWND still belongs to the same owning process, which can preserve stale metadata after missed destroy notifications or HWND reuse.

## 2026-05-29 Finder Freeze Findings
- User confirmation after attempt 007: tree view is fixed, finder is partially fixed and much better, but finder selection freezes the UI.
- `FocusWindowInTree` still ran `ControlProxy.FromHandle`, managed ancestor checks, top-level window enumeration, parent-chain construction, and immediate child enumeration on the UI thread.
- `treeWindow_AfterSelect` always called `UpdateLayoutTab`, and `UpdateLayoutTab` synchronously called `ControlProxy.GetLayoutInfo()` even when the Layout tab was not visible.
- `FlashWindowHandle` used `Thread.Sleep` in a loop on the UI thread after finder selection.
