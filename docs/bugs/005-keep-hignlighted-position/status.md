# Bug Status

## Current State
awaiting-user-confirmation

## Active Attempt
`fix-attempt-003.md`

## Last Updated
2026-05-27

## Confirmation Date
Lifecycle positioning fixed by user retest on 2026-05-27; duplicate tree item follow-up pending

## Resolution Summary
Diagnostic instrumentation has been implemented and validated. The first user retest with the diagnostic build appeared correct, but that attempt did not intentionally change highlight selection behavior. The user then found a lifecycle pattern: highlighting works on the first Settings dialog instance and breaks after closing and reopening Settings. Attempt 002 invalidates persistent highlight and stale tree nodes when target handles are destroyed; the user confirmed this improved/fixed positioning but found duplicate tree items. Attempt 003 addresses the duplicate tree nodes.

## Attempt History
- Research intake created after `docs\bugs\003-tree-subtree-refresh-highlight\fix-attempt-028.md` failed user retesting.
- `fix-attempt-001.md` - implemented diagnostic instrumentation, awaiting user diagnostics
- `fix-attempt-002.md` - implemented target-handle lifecycle invalidation; user confirmed positioning is fixed/better
- `fix-attempt-003.md` - implemented duplicate tree node cleanup, awaiting user confirmation

## State Change Log
- 2026-05-27: bug opened from user follow-up that `Keep Highlighted` still has invalid position for `button_EmSync_Login`.
- 2026-05-27: user requested a new bug report named `keep-hignlighted-position`.
- 2026-05-27: GPT and Opus research captured in `research.md`.
- 2026-05-27: research report completed; awaiting user confirmation/follow-up before implementation.
- 2026-05-27: user approved proceeding with diagnostic instrumentation.
- 2026-05-27: attempt 001 started for diagnostic instrumentation.
- 2026-05-27: attempt 001 implemented diagnostic-only logging.
- 2026-05-27: build and x86/x64 tests passed.
- 2026-05-27: awaiting user diagnostic retest.
- 2026-05-27: user reported the diagnostic build currently highlights at the right position; recorded as inconclusive because the attempt is diagnostic-only.
- 2026-05-27: user reported the key lifecycle clue: first Settings open works, second Settings open after close/reopen breaks.
- 2026-05-27: research updated with stale proxy/handle invalidation as the recommended next fix.
- 2026-05-27: user approved implementing lifecycle invalidation.
- 2026-05-27: attempt 002 started.
- 2026-05-27: attempt 002 implemented target-handle lifecycle invalidation.
- 2026-05-27: build, x86/x64 tests, and focused code review passed.
- 2026-05-27: awaiting user lifecycle retest.
- 2026-05-27: user confirmed lifecycle positioning is fixed/better, but reported duplicate tree items.
- 2026-05-27: attempt 003 started for duplicate tree node cleanup.
- 2026-05-27: attempt 003 implemented sibling handle de-duplication during tree population and handle-change updates.
- 2026-05-27: build passed; initial test run hit a UIAutomation startup timeout, but rerun x86/x64 tests passed.
- 2026-05-27: awaiting user duplicate-tree retest.

## Notes
- The user requested research and confirmation before proceeding to implementation.
- Keep this bug open until user confirmation after a future fix attempt.
