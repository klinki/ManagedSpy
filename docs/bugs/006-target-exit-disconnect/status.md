# Bug Status

## Current State
fixed

## Active Attempt
`fix-attempt-001.md`

## Last Updated
2026-05-28

## Confirmation Date
2026-05-28

## Resolution Summary
- Attempt 001 is locally implemented and validated. ManagedSpy now avoids path-locking target assemblies during metadata loading and removes tracked target process state when the target exits.
- User confirmed the target-exit disconnect and file unlock behavior is fixed.

## Attempt History
- `fix-attempt-001.md` - in progress for no-lock target assembly metadata loading and process-exit disconnect.
- `fix-attempt-001.md` - awaiting user confirmation after build and x86/x64 artifact tests passed.
- `fix-attempt-001.md` - fixed after user confirmation.

## State Change Log
- 2026-05-27: bug opened from user report that ManagedSpy keeps target files locked after the target app exits.
- 2026-05-27: investigation found `Assembly.LoadFile` target metadata loading and missing target process exit cleanup.
- 2026-05-27: attempt 001 started.
- 2026-05-28: attempt 001 implemented no-lock assembly loading, process-exit disconnect, tracked process disposal, stale proxy cache cleanup, and classification cache cleanup.
- 2026-05-28: focused review found proxy cache cleanup must use captured process ownership rather than live HWND process lookup after target exit; attempt 001 was updated accordingly.
- 2026-05-28: build and x86/x64 artifact tests passed; awaiting user confirmation.
- 2026-05-28: user confirmed the fix.

## Notes
- Keep watching byte-loaded target assembly metadata behavior during future property/event browsing changes.
