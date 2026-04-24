# Fix Attempt 001

## Attempt Status
fixed

## Goal
Make finder highlight stable and monitor-correct, with guaranteed cleanup when finder is disabled.

## Relation To Previous Attempts
First attempt for this bug.

## Proposed Change
- Replace reversible-frame highlighting with a dedicated transparent topmost overlay window that draws only a border.
- Use Win32 `GetCursorPos` for cursor coordinates to align with Win32 window rectangles.
- Reuse the same overlay for pulse/flash highlighting to eliminate XOR artifacts.

## Risks
- Overlay window might interfere with hit-testing unless explicitly mouse-transparent.
- Possible timing/flicker issues if overlay updates too frequently.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\002-element-finder-overlay-artifacts\status.md`

## Verification Plan
- Build solution for `Release|x64` and `Release|x86`.
- Smoke-test startup to ensure no regressions.

## Implementation Summary
- Replaced XOR-based `ControlPaint.DrawReversibleFrame` highlight rendering with a dedicated transparent overlay form (`HighlightOverlayForm`).
- Overlay is always topmost, non-activating, and hit-test transparent (`WM_NCHITTEST -> HTTRANSPARENT`) so it does not interfere with picking.
- Finder highlight now updates by moving/resizing the overlay around the target window rectangle, then hiding it cleanly on stop.
- Cursor position for hit-testing now uses Win32 `GetCursorPos` to keep coordinate space aligned with `GetWindowRect`.
- `FlashWindowHandle` now reuses the overlay pulse instead of reversible frame drawing, removing XOR artifacts there as well.

## Test Results
- `.\build.ps1` succeeded for Release x86 and x64.
- Startup smoke test from `artifacts\release\x64\ManagedSpy.exe` stayed alive during initial scan window.

## Outcome
Local fix is implemented and verified for build/startup. The rendering path no longer relies on reversible frame XOR drawing, which should eliminate lingering artifacts and wrong-screen drawing issues.

## Next Step
Completed: user retested and confirmed the issue is fixed.

## Remaining Gaps
- User-side validation still required for multi-monitor/high-DPI environment.
