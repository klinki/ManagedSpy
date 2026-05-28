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

## 2026-04-28 Regression Addendum

### Confirmed Facts
- Attempt 023 fixed stale overlay-window reuse on target switches, but the user has now reported a renewed eM Client-specific offset with a single selected label.
- The new screenshot shows `label_EmSyncImportAccounts_Description` selected in the tree while the persistent rectangle is rendered much lower on the same page.
- `MainForm.TryGetPersistentHighlightRectangle()` currently requests `proxy.GetScreenBounds(false)` only when a previous rectangle exists, so first-selection updates still trust the preferred/accessibility path outright.
- `ScreenBoundsHelper::GetControlScreenBounds(..., preferAccessibility: true)` currently keeps non-empty accessibility bounds without validating that they still line up with the selected control's native client rectangle.

### Likely Cause
- Some eM Client controls appear to expose accessibility-derived bounds that survive the existing DPI normalization but do not match the control's actual on-screen client rectangle on first selection.

### Unknowns
- Whether the non-accessibility/native client bounds fully match the selected eM Client label in the user's environment.
- Whether the renewed offset is entirely explained by the accessibility path or still needs another DPI-space adjustment after fallback.

### Reproduction Status
- Reproduced indirectly from the user's screenshot and code inspection; direct local reproduction in eM Client is not available in this environment.

### Evidence Gathered
- `screenshots\bug_dpi_issues.png`
- `ManagedSpy\MainForm.cs` persistent-highlight rectangle selection logic
- `ManagedSpyLib\ScreenBoundsHelper.cpp` accessibility/native bounds selection and DPI normalization
- bug 03 attempt history through `fix-attempt-023.md`

## 2026-04-28 Diagnostic Follow-up

### Confirmed Facts
- User retest after attempt 024 still reproduced the offset in eM Client (`screenshots\bug_dpi_02.png`).
- The new diagnostic log shows `preferred` and `nonAccessible` rectangles being multiplied by the same 1.75x factor relative to the inspector-side raw/root rectangles.
- Example evidence from the user log:
  - `raw=438,528,795,69` versus `preferred=766,924,1391,120`
  - `raw=414,268,842,695` versus `preferred=726,528,1471,1063`
- Because the same over-scaling appears on `preferAccessibility=false`, the renewed bug is no longer isolated to accessibility selection; it points at the shared DPI-normalization step.

### Likely Cause
- `NormalizeManagedScreenBounds` is sometimes converting native client rectangles that are already in usable screen coordinates, which double-scales them by the monitor DPI factor.

### Unknowns
- Whether any target application still needs DPI normalization on the non-accessibility/native path after this over-scaling is removed.

### Reproduction Status
- User reproduced the renewed bug after attempt 024; direct local reproduction in eM Client is still unavailable in this environment.

### Evidence Gathered
- `screenshots\bug_dpi_02.png`
- user-provided diagnostic log excerpt from 2026-04-28 showing exact 1.75x scaling on both preferred and non-accessible rectangles
- `ManagedSpyLib\ScreenBoundsHelper.cpp`

## 2026-04-28 Cross-Process DPI Addendum

### Confirmed Facts
- User retest after attempt 025 still reproduced the same oversized/off-position rectangle (`screenshots\bug_dpi_03.png`).
- The new diagnostics still show `preferred` and `nonAccessible` rectangles exactly 1.75x larger than the locally observed raw/root rectangles, even after the native branch stopped using target-process normalization.
- `ManagedSpy\Program.cs` and `ManagedSpy\app.config` contain no explicit DPI-awareness setup or manifest entries.

### Likely Cause
- eM Client appears to be reporting rectangles in a DPI-aware screen coordinate space that ManagedSpy's current overlay process does not use directly, so the overlay needs a local-space conversion step before drawing.

### Unknowns
- Whether local overlay-space normalization is enough for all target apps, or if ManagedSpy eventually needs to become DPI-aware itself.

### Reproduction Status
- User reproduced again after attempt 025; direct local reproduction in eM Client is still unavailable in this environment.

### Evidence Gathered
- `screenshots\bug_dpi_03.png`
- user-provided diagnostic log excerpt from 2026-04-28 after attempt 025
- `ManagedSpy\Program.cs`
- `ManagedSpy\app.config`

## 2026-04-28 Raw-Fallback Addendum

### Confirmed Facts
- User retest after attempt 026 still reproduced the same offset, and the diagnostic rectangles remained unchanged.
- Example evidence from the latest log:
  - `preferred=2361,1570,122,54` versus `raw=1349,897,70,31`
  - `preferred=1224,755,1391,120` versus `raw=699,431,795,69`
- In both examples, the preferred rectangle remains a near-perfect 1.75x scale-up of the local raw rectangle even after the local-space conversion attempt.

### Likely Cause
- The API-based local-space conversion attempt is not changing the coordinate space the way this app combination needs, but the local raw Win32 rectangle remains stable and already aligned with the user's visible control.

### Unknowns
- Whether the raw-window fallback stays safe for targets where the proxy handle still points at an ancestor/native container instead of the exact managed child.

### Reproduction Status
- User reproduced again after attempt 026; direct local reproduction in eM Client is still unavailable in this environment.

### Evidence Gathered
- user-provided diagnostic log excerpt from 2026-04-28 after attempt 026
- `ManagedSpy\MainForm.cs`

## 2026-05-27 Renewed Invalid-Location Addendum

### Confirmed Facts
- The user reported that the invalid-location issue is present again for `Keep Highlighted`.
- `screenshots\keep_highlighted_invalid_location.png` shows `optionButton_CustomSetup` selected in ManagedSpy while the red persistent-highlight rectangle is drawn much lower and wider than the selected option item.
- The current fallback requires the raw rectangle to have a larger absolute intersection area with the root window than the managed candidate rectangle.
- An oversized DPI-scaled candidate can still overlap a large enough part of the root window to beat the raw rectangle by absolute intersection area, even while a smaller raw rectangle is much better contained by the root.

### Likely Cause
- The attempt 027 fallback still uses absolute intersection area as the final "fits root better" test. That is too weak for a scaled candidate that spills far outside the target root but has a larger area overall.

### Unknowns
- Whether the user's exact eM Client coordinates match the screenshot-derived shape closely enough for a deterministic local test to cover the same branch.

### Reproduction Status
- Reproduced indirectly from the user's screenshot and code inspection; direct local reproduction in eM Client is not available in this environment.

### Evidence Gathered
- `screenshots\keep_highlighted_invalid_location.png`
- `ManagedSpyLib\ScreenBoundsHelper.cs`
- `ManagedSpy.Tests\ScreenBoundsHelperTests.cs`
