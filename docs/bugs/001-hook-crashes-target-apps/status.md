# Bug Status

## Current State
awaiting-user-confirmation

## Active Attempt
`fix-attempt-001.md`

## Last Updated
2026-04-24

## Confirmation Date
pending

## Resolution Summary
Hook eligibility is now runtime-aware. ManagedSpy no longer treats every managed process as injectable; it only injects into compatible modern .NET runtimes (version 10+), and skips .NET Framework/incompatible runtimes.

## Attempt History
- `fix-attempt-001.md` - implemented and locally verified, awaiting user confirmation

## State Change Log
- 2026-04-24: bug opened from user report about target-app crash after ManagedSpy startup
- 2026-04-24: investigation completed, root cause identified as hook/runtime compatibility mismatch
- 2026-04-24: attempt 001 started
- 2026-04-24: attempt 001 implemented (runtime compatibility guard in `Commands.cpp`)
- 2026-04-24: local build verification finished for Release x64/x86
- 2026-04-24: awaiting user confirmation

## Notes
- This bug tracks one regression introduced by migration to .NET 10: incompatible hook injection into external managed runtimes.
