# Bug Status

## Current State
awaiting-user-confirmation

## Active Attempt
`fix-attempt-011.md`

## Last Updated
2026-04-27

## Confirmation Date
pending

## Resolution Summary
Added component-tree context-menu actions for subtree refresh and persistent highlight, corrected persistent highlight z-order, strengthened handle-change propagation, bound tree context-menu actions to the exact node that opened the menu, switched persistent highlight to a test-backed target-process screen-bounds helper, clipped child-control highlight rectangles to ancestor client bounds, and moved child-control positioning onto native HWND screen coordinates.

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

## Notes
- This bug also includes UX enhancement requested by the user (persistent highlight toggle in tree context menu).
