# Bug Status

## Current State
awaiting-user-confirmation

## Active Attempt
`fix-attempt-002.md`

## Last Updated
2026-04-24

## Confirmation Date
pending

## Resolution Summary
Added component-tree context-menu actions for subtree refresh and persistent highlight, then adjusted persistent highlight z-order so it no longer renders above unrelated overlapping windows.

## Attempt History
- `fix-attempt-001.md` - implemented and locally verified, awaiting user confirmation
- `fix-attempt-002.md` - implemented z-order correction for persistent highlight, awaiting user confirmation

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

## Notes
- This bug also includes UX enhancement requested by the user (persistent highlight toggle in tree context menu).
