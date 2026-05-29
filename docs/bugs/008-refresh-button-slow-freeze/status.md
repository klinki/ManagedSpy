# Bug Status

## Current State
fixed

## Active Attempt
`fix-attempt-001.md`

## Last Updated
2026-05-29

## Confirmation Date
2026-05-29

## Resolution Summary
- Attempt 001 moves expensive refresh discovery and metadata reads off the UI thread, keeps only final tree application on the UI thread, and removes the `ComponentType` byte-load path from routine managed filtering.
- User confirmed Refresh is now fixed/much better.

## Attempt History
- `fix-attempt-001.md` - awaiting user confirmation after background refresh and refresh fast-path cleanup.
- `fix-attempt-001.md` - user confirmed fixed.

## State Change Log
- 2026-05-29: bug opened from user report that Refresh became much slower and freezes the UI.
- 2026-05-29: investigation found synchronous UI-thread refresh and unnecessary `ComponentType` resolution in the refresh managed-filter path.
- 2026-05-29: attempt 001 started.
- 2026-05-29: attempt 001 implementation, build, and x86/x64 artifact tests passed; awaiting user confirmation.
- 2026-05-29: user confirmed Refresh is fixed/much better.

## Notes
- Confirmed fixed by user on 2026-05-29.
