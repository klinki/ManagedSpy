# Bug Status

## Current State
fixed

## Active Attempt
`fix-attempt-027.md`

## Last Updated
2026-04-28

## Confirmation Date
2026-04-28

## Resolution Summary
Attempt 023 fixed stale overlay reuse on target switches, but the bug reopened in eM Client on 2026-04-28 with a renewed geometry mismatch: the persistent highlight rectangle could land noticeably below the selected control. Attempts 024-026 narrowed the problem to a repeated 1.75x DPI-scale mismatch between managed target rectangles and the local overlay coordinate space. Attempt 027 resolved that by falling back to the already-correct local raw Win32 rectangle only when the managed rectangle is a near-uniform DPI-scaled twin of it. The user confirmed this final attempt fixed the issue in eM Client.

## Attempt History
- `fix-attempt-001.md` - implemented and locally verified, awaiting user confirmation
- `fix-attempt-002.md` - implemented z-order correction for persistent highlight, awaiting user confirmation
- `fix-attempt-003.md` - implemented proxy-based handle tracking for persistent highlight, awaiting user confirmation
- `fix-attempt-004.md` - implemented handle-change cache hardening for persistent highlight, awaiting user confirmation
- `fix-attempt-005.md` - implemented managed-bounds-based persistent highlight updates, awaiting user confirmation
- `fix-attempt-006.md` - reverted managed-bounds geometry and aligned persistent highlight with magnifier rectangle handling, awaiting user confirmation
- `fix-attempt-007.md` - bound tree context-menu actions to the explicit right-clicked node, awaiting user confirmation
- `fix-attempt-008.md` - queried target-process screen bounds for persistent highlight, awaiting user confirmation
- `fix-attempt-009.md` - extracted and tested the target-process screen-bounds helper, awaiting user confirmation
- `fix-attempt-010.md` - clipped target-process child bounds to ancestor client rectangles, awaiting user confirmation
- `fix-attempt-011.md` - mapped child-control screen bounds through native HWND coordinates, awaiting user confirmation
- `fix-attempt-012.md` - preferred accessibility bounds for custom child controls, awaiting user confirmation
- `fix-attempt-013.md` - resolved highlight targets by managed control path instead of HWND alone, awaiting user confirmation
- `fix-attempt-014.md` - normalized managed highlight rectangles for likely DPI-space mismatches, awaiting user confirmation
- `fix-attempt-015.md` - deferred tree-menu highlight actions until after the menu closes, awaiting user confirmation
- `fix-attempt-016.md` - requested a redraw when switching highlighted targets, awaiting user confirmation
- `fix-attempt-017.md` - fell back from stale accessibility rectangles on target switches, awaiting user confirmation
- `fix-attempt-018.md` - added persistent highlight diagnostics logging, awaiting user diagnostics
- `fix-attempt-019.md` - preferred the non-accessibility path on target switches, awaiting user confirmation
- `fix-attempt-020.md` - repainted the overlay synchronously after each move, awaiting user confirmation
- `fix-attempt-021.md` - expanded diagnostics with raw and root window rectangles, awaiting user diagnostics
- `fix-attempt-022.md` - redrew old and new target roots after the overlay move, awaiting user confirmation
- `fix-attempt-023.md` - recreated the persistent overlay window on target switches, awaiting user confirmation
- `fix-attempt-024.md` - implemented accessibility/native bounds plausibility guard; user reported bug still present and diagnostics showed shared 1.75x over-scaling
- `fix-attempt-025.md` - implemented native-path DPI-normalization change; user reported bug still present and diagnostics still showed the same 1.75x overshoot
- `fix-attempt-026.md` - implemented local overlay-space normalization; user reported bug still present and diagnostics did not change
- `fix-attempt-027.md` - implemented local raw-window fallback for repeated DPI-scale mismatch, awaiting user confirmation

## State Change Log
- 2026-04-24: bug opened from user report about missing dynamically added descendants in the tree
- 2026-04-24: investigation completed and likely cause identified
- 2026-04-24: attempt 001 started
- 2026-04-24: attempt 001 implemented (refresh subtree + keep highlighted toggle)
- 2026-04-24: build and startup smoke verification passed
- 2026-04-24: awaiting user confirmation
- 2026-04-24: user reported persistent highlight still incorrect when another window overlaps target
- 2026-04-24: attempt 002 started
- 2026-04-24: attempt 002 implemented (persistent highlight now follows target-window z-order)
- 2026-04-24: build and startup smoke verification passed
- 2026-04-24: awaiting user confirmation
- 2026-04-24: user reported stale highlight position during drag/drop in some scenarios
- 2026-04-24: attempt 003 started
- 2026-04-24: attempt 003 implemented (persistent highlight now tracks live proxy handle)
- 2026-04-24: build and startup smoke verification passed
- 2026-04-24: awaiting user confirmation
- 2026-04-24: user reported stale post-drop position until clicking target window again
- 2026-04-24: attempt 004 started
- 2026-04-24: attempt 004 implemented (handle-change propagation/cache update hardening)
- 2026-04-24: build and startup smoke verification passed
- 2026-04-24: awaiting user confirmation
- 2026-04-24: user reported no change after attempt 004; stale post-drop position persisted
- 2026-04-24: attempt 005 started
- 2026-04-24: attempt 005 implemented (persistent highlight now prefers managed bounds over raw HWND rectangles)
- 2026-04-24: build and startup smoke verification passed
- 2026-04-24: awaiting user confirmation
- 2026-04-27: user reported attempt 005 geometry was completely off and shared screenshot showing mismatch versus working magnifier highlight
- 2026-04-27: attempt 006 started
- 2026-04-27: attempt 006 implemented (persistent highlight now uses magnifier-style HWND rectangles again at 80 ms cadence)
- 2026-04-27: build and startup smoke verification passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported `Keep Highlighted` still followed the last magnifier-selected component instead of the context-menu tree node
- 2026-04-27: attempt 007 started
- 2026-04-27: attempt 007 implemented (tree context-menu actions now target the explicit right-clicked node)
- 2026-04-27: build and startup smoke verification passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported deep tree components could still highlight an ancestor container rather than the selected component
- 2026-04-27: attempt 008 started
- 2026-04-27: attempt 008 implemented (persistent highlight now queries selected control screen bounds from the spied process)
- 2026-04-27: build and startup smoke verification passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported attempt 008 was completely broken and could highlight something far off-screen; requested automated tests
- 2026-04-27: attempt 009 started
- 2026-04-27: attempt 009 implemented (target-process screen-bounds helper extracted and covered by automated tests)
- 2026-04-27: build, tests, and startup smoke verification passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported attempt 009 was still absolutely off and shared screenshot evidence of an oversized/off-screen persistent rectangle
- 2026-04-27: attempt 010 started
- 2026-04-27: attempt 010 implemented (target-process child bounds now clip to ancestor client rectangles)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported attempt 010 improved rectangle shape but it was still offset significantly and shared a second screenshot
- 2026-04-27: attempt 011 started
- 2026-04-27: attempt 011 implemented (child-control bounds now map through native HWND coordinates before clipping)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported attempt 011 was still broken with the same behavior
- 2026-04-27: attempt 012 started
- 2026-04-27: attempt 012 implemented (child-control highlights now prefer accessibility bounds before native/window fallbacks)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported attempt 012 showed no visible change
- 2026-04-27: diagnostic retest showed `Show Window` did not visibly highlight the target and `Refresh Subtree` made no difference
- 2026-04-27: attempt 013 started
- 2026-04-27: attempt 013 implemented (highlight target now resolves through the proxy's managed child path before bounds lookup)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported attempt 013 was still broken, but behavior varied by app and seemed much worse in eM Client than in ManagedSpy itself
- 2026-04-27: attempt 014 started
- 2026-04-27: attempt 014 implemented (managed/accessibility rectangles now normalize to physical screen coordinates when they do not fit the real top-level window)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported attempt 014 was much better and almost correct, but the previous component stayed highlighted until clicking the target app triggered a redraw
- 2026-04-27: attempt 015 started
- 2026-04-27: attempt 015 implemented (tree context-menu highlight actions now defer until after the menu closes)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported attempt 015 did not change the behavior; first selection worked, but following selections still needed a click in the target app to redraw
- 2026-04-27: attempt 016 started
- 2026-04-27: attempt 016 implemented (switching highlighted targets now requests a redraw from the target root window)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported attempt 016 still required a click in the target app before the new highlighted component appeared
- 2026-04-27: attempt 017 started
- 2026-04-27: attempt 017 implemented (target switches now retry without accessibility when the first returned rectangle matches the previous highlight)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported no change after attempt 017
- 2026-04-27: attempt 018 started
- 2026-04-27: attempt 018 implemented (persistent highlight now writes diagnostic candidate-rectangle logs next to the executable)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user diagnostics
- 2026-04-27: user shared diagnostic log data showing the preferred rectangle being chosen first on a bad switch and later drifting after the target app was clicked
- 2026-04-27: attempt 019 started
- 2026-04-27: attempt 019 implemented (target switches now prefer the non-accessibility path when it can immediately replace the old rectangle)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported no visible change after attempt 019 and shared a diagnostic log showing the chosen rectangle already changing immediately
- 2026-04-27: attempt 020 started
- 2026-04-27: attempt 020 implemented (overlay now repaints synchronously after each move)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported no visible change after attempt 020 and shared a diagnostic log showing the chosen rectangle still changing after the target app click
- 2026-04-27: attempt 021 started
- 2026-04-27: attempt 021 implemented (diagnostics now log raw target-window and root-window rectangles on every update)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user diagnostics
- 2026-04-27: user shared the expanded diagnostic log showing raw/root/chosen rectangles already stable at switch time
- 2026-04-27: attempt 022 started
- 2026-04-27: attempt 022 implemented (old/new target roots now redraw again after the overlay move)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation
- 2026-04-27: user reported no visible change after attempt 022
- 2026-04-27: attempt 023 started
- 2026-04-27: attempt 023 implemented (persistent highlight now recreates the overlay window on target switches)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation
- 2026-04-28: user reported bug 03 back in eM Client and shared `screenshots\bug_dpi_issues.png`, showing the persistent highlight rectangle noticeably below the selected label
- 2026-04-28: attempt 024 started
- 2026-04-28: attempt 024 implemented (fall back from implausible accessibility bounds to native client geometry)
- 2026-04-28: build and tests passed
- 2026-04-28: awaiting user confirmation
- 2026-04-28: user reported attempt 024 still broken in eM Client and shared `screenshots\bug_dpi_02.png` plus diagnostics showing preferred and non-accessible rectangles scaled by 1.75x
- 2026-04-28: attempt 025 started
- 2026-04-28: attempt 025 implemented (native client rectangles no longer flow through shared DPI normalization)
- 2026-04-28: build and tests passed
- 2026-04-28: awaiting user confirmation
- 2026-04-28: user reported attempt 025 still broken in eM Client and shared `screenshots\bug_dpi_03.png` plus diagnostics still showing the same 1.75x overshoot
- 2026-04-28: attempt 026 started
- 2026-04-28: attempt 026 implemented (target-process rectangles now normalize into ManagedSpy's local overlay coordinate space when that better fits the local root window bounds)
- 2026-04-28: build and tests passed
- 2026-04-28: awaiting user confirmation
- 2026-04-28: user reported attempt 026 still broken in eM Client; logs still showed the same 1.75x overshoot and no visible change
- 2026-04-28: attempt 027 started
- 2026-04-28: attempt 027 implemented (fall back to the local raw window rectangle when the managed rectangle matches a repeated DPI-scale signature)
- 2026-04-28: build and tests passed
- 2026-04-28: awaiting user confirmation
- 2026-04-28: user confirmed attempt 027 fixed the issue in eM Client
- 2026-04-28: extracted the raw-window DPI fallback heuristic into `ManagedSpyLib.ScreenBoundsHelper` and added deterministic regression tests for the logged 1.75x mismatch cases
- 2026-04-27: user confirmed attempt 023 fixed the issue
- 2026-04-27: user reported no visible change after attempt 022
- 2026-04-27: attempt 023 started
- 2026-04-27: attempt 023 implemented (persistent highlight now recreates the overlay window on target switches)
- 2026-04-27: build and tests passed
- 2026-04-27: awaiting user confirmation

## Notes
- This bug also includes UX enhancement requested by the user (persistent highlight toggle in tree context menu).
