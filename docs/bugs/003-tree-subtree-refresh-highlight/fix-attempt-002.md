# Fix Attempt 002

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Ensure persistent highlight does not render above unrelated windows that are layered above the target control.

## Relation To Previous Attempts
Follow-up to `fix-attempt-001.md`, which added persistent highlighting but used a topmost overlay.

## Proposed Change
- Keep finder/flash overlay behavior unchanged.
- Make persistent highlight overlay non-topmost.
- Reposition persistent overlay in z-order relative to the target window's top-level root so it stays above the target but below windows above it.

## Risks
- Z-order behavior can vary across special window types (topmost/tool windows).
- Frequent repositioning must avoid flicker.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Startup smoke run of the produced x64 binary.

## Implementation Summary
- Added Win32 z-order helpers (`GetAncestor`, `GetWindow`) and root-window constants in `MainForm`.
- Changed persistent highlight overlay to `new HighlightOverlayForm(false)` so it is not always topmost.
- Updated persistent highlight refresh to compute z-order insertion point from the highlighted target's root window (`GW_HWNDPREV`) and apply it on each timer tick.
- Extended `HighlightOverlayForm` with configurable topmost behavior and an overload `ShowHighlight(Rectangle, IntPtr insertAfterWindow)`.
- Kept finder and flash overlays on the existing topmost behavior.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out` succeeded for Release x86/x64.
- Startup smoke run from x64 build output stayed alive during initial window scan.

## Outcome
Persistent highlight now follows target-window z-order instead of always drawing on top of the desktop.

## Next Step
User confirmation in the reported overlapping-window scenario.

## Remaining Gaps
- Behavioral confirmation in the user environment is still pending.
