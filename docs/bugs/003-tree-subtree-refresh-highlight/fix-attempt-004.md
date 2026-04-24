# Fix Attempt 004

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Fix remaining stale highlight cases where drag/drop leaves highlight at old position until a later click.

## Relation To Previous Attempts
Follow-up to `fix-attempt-003.md`, which moved tracking to proxy-based handles but still relied on handle-change propagation that could miss cache edge-cases.

## Proposed Change
- Correct cache update logic in `ControlProxy::OnHandleCreated`.
- Harden `WM_HANDLECHANGED` handling in `EventTargetWindow` to recover when old-handle key lookup fails.
- Remove stale proxy keys and rebind proxy to the new handle deterministically.

## Risks
- Aggressive cache cleanup during handle-change must avoid removing unrelated proxies.
- Behavior depends on stable delivery of custom handle-change messages.

## Files And Components
- `ManagedSpyLib\ControlProxy.cpp`
- `ManagedSpyLib\EventTargetWindow.cpp`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Startup smoke run of the produced x64 binary.

## Implementation Summary
- Fixed `ControlProxy::OnHandleCreated` cache cleanup condition to check and remove `oldHandle` (instead of incorrectly checking `w->Handle`).
- Updated `EventTargetWindow` `WM_HANDLECHANGED` path to:
  - look up proxy by old-handle key,
  - fallback to search by proxy value handle when key lookup misses,
  - remove stale keys tied to that proxy,
  - set proxy handle to new value and reinsert under the new key.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out` succeeded for Release x86/x64.
- Startup smoke run from x64 build output stayed alive during initial window scan.

## Outcome
Handle-change propagation is now more resilient, reducing stale highlight attachment after drag/drop handle transitions.

## Next Step
User confirmation in the reported drag/drop scenario.

## Remaining Gaps
- Behavioral confirmation in the user environment is still pending.
