# Fix Attempt 005

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Make persistent highlight follow the control's post-drop position even when the HWND rectangle lags behind managed layout state.

## Relation To Previous Attempts
Follow-up to `fix-attempt-004.md`, which hardened handle-change propagation but did not address stale rectangle updates where the same control remained selected.

## Proposed Change
- Keep z-order and handle-tracking improvements from prior attempts.
- For persistent highlight, prefer managed control `Bounds` combined with the parent client's screen origin.
- Fall back to `GetWindowRect` only when managed bounds are unavailable.

## Risks
- Managed bounds may differ from HWND bounds for some nonstandard/native controls.
- Parent client origin lookup must remain stable during reparenting.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Startup smoke run of the produced x64 binary.

## Implementation Summary
- Added `ClientToScreen` P/Invoke in `MainForm`.
- Added `TryGetPersistentHighlightRectangle`, `TryGetManagedHighlightRectangle`, and `TryGetManagedBounds` helpers.
- Updated persistent highlight refresh to prefer `proxy.GetValue("Bounds")` plus the parent control's client-screen origin.
- Kept fallback to `GetWindowRect` for cases where managed bounds are not available.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out` succeeded for Release x86/x64.
- Startup smoke run from x64 build output stayed alive during initial window scan.

## Outcome
Persistent highlight now prefers managed layout bounds over raw window rectangles, which should better reflect post-drop positions for managed controls.

## Follow-up Update
- 2026-04-27: user reported this approach produced incorrect highlight geometry in a real application even though the magnifier highlight remained correct.
- Superseded by `fix-attempt-006.md`, which restores HWND-based geometry for persistent highlight.

## Next Step
User confirmation in the original drag/drop scenario.

## Remaining Gaps
- Behavioral confirmation in the user environment is still pending.
