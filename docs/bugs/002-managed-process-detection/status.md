# Bug Status

## Current State
- fixed

## Active Attempt
- `fix-attempt-002.md`

## Last Updated
- 2026-04-28

## Confirmation Date
- 2026-04-28

## Resolution Summary
- `ManagedSpy\ManagedSpy.csproj` now restores the hook shim dependency for MSBuild/Visual Studio builds, so `ManagedSpyHook.dll` is produced beside the normal app output again.
- The supported solution build/test flow still passes after the wiring change.
- That deployment fix was not sufficient: user retesting still reports failure, now with a freeze when another ManagedSpy instance starts.
- `ManagedSpyHook.dll` now resolves `ManagedSpyLib.dll` from its own directory instead of by simple assembly name, and the managed transport now uses timeout-based sends to avoid indefinite cross-process hangs.
- User confirmed that the two-instance scenario now works and managed detection is restored.

## Attempt History
- `fix-attempt-001.md` - restore hook shim build/deployment for normal app builds.
- `fix-attempt-002.md` - load the managed bridge from the hook DLL directory and add message-send timeouts to avoid cross-process hangs.

## State Change Log
- 2026-04-28: bug opened from user report that the ported app only sees native windows.
- 2026-04-28: investigation confirmed the normal app output is missing `ManagedSpyHook.dll`.
- 2026-04-28: fix attempt 001 started.
- 2026-04-28: local verification passed; awaiting user confirmation.
- 2026-04-28: user reported the bug is still present and the app freezes when another ManagedSpy instance starts.
- 2026-04-28: fix attempt 002 started.
- 2026-04-28: second-attempt local verification passed; awaiting user confirmation.
- 2026-04-28: user confirmed the fix in the real two-instance scenario.

## Notes
- Closed after explicit user confirmation.
