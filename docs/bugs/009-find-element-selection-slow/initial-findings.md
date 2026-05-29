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
