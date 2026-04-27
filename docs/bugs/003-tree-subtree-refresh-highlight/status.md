# Bug Status

## Current State
awaiting-user-confirmation

## Active Attempt
`fix-attempt-006.md`

## Last Updated
2026-04-27

## Confirmation Date
pending

## Resolution Summary
Added component-tree context-menu actions for subtree refresh and persistent highlight, corrected persistent highlight z-order, strengthened handle-change propagation, and realigned persistent highlight geometry with the magnifier's HWND-based rectangle path after a managed-bounds regression.

## Attempt History
- `fix-attempt-001.md` - implemented and locally verified, awaiting user confirmation
- `fix-attempt-002.md` - implemented z-order correction for persistent highlight, awaiting user confirmation
- `fix-attempt-003.md` - implemented proxy-based handle tracking for persistent highlight, awaiting user confirmation
- `fix-attempt-004.md` - implemented handle-change cache hardening for persistent highlight, awaiting user confirmation
- `fix-attempt-005.md` - implemented managed-bounds-based persistent highlight updates, awaiting user confirmation
- `fix-attempt-006.md` - reverted managed-bounds geometry and aligned persistent highlight with magnifier rectangle handling, awaiting user confirmation

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

## Notes
- This bug also includes UX enhancement requested by the user (persistent highlight toggle in tree context menu).
