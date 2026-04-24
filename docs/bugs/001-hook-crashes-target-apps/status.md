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
1. Hook eligibility is runtime-aware: only compatible modern .NET runtimes are considered hook targets.
2. Process/module inspection failures (including `Win32Exception` access denied) are now handled safely, so startup scan skips inaccessible processes instead of crashing ManagedSpy.

## Attempt History
- `fix-attempt-001.md` - implemented and locally verified, awaiting user confirmation
- `fix-attempt-002.md` - created after user reported new ManagedSpy startup crash (`Win32Exception: Access denied`)

## State Change Log
- 2026-04-24: bug opened from user report about target-app crash after ManagedSpy startup
- 2026-04-24: investigation completed, root cause identified as hook/runtime compatibility mismatch
- 2026-04-24: attempt 001 started
- 2026-04-24: attempt 001 implemented (runtime compatibility guard in `Commands.cpp`)
- 2026-04-24: local build verification finished for Release x64/x86
- 2026-04-24: awaiting user confirmation
- 2026-04-24: user reported ManagedSpy crash in `Desktop::IsManagedProcess` due `Process.Modules` access denied
- 2026-04-24: attempt 002 started
- 2026-04-24: attempt 002 implemented (guarded process/module inspection exceptions)
- 2026-04-24: attempt 002 local verification passed (build + startup/regression smoke tests)
- 2026-04-24: awaiting user confirmation (attempt 002)

## Notes
- This bug tracks one regression introduced by migration to .NET 10: incompatible hook injection into external managed runtimes.
