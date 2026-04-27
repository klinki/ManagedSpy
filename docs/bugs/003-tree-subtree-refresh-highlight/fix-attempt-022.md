# Fix Attempt 022

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Invalidate the old/new target roots after the overlay has actually moved so stale pixels from the previous frame get cleared on the same switch operation.

## Relation To Previous Attempts
Follow-up to `fix-attempt-021.md`. The expanded diagnostics showed that:
- raw/root/chosen rectangles were already stable at switch time,
- yet the visible highlight still lagged until a click in the target app.

That strongly suggests the remaining problem is stale on-screen pixels from the old frame rather than late geometry selection.

## Proposed Change
- Keep the pre-move redraw request.
- Also request redraws again after the overlay has moved, for both:
  - the previous target root
  - the current target root

## Risks
- Extra redraw requests may slightly increase visual work during highlight switches.
- This still targets the repaint/cleanup path, not the geometry path.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the existing helper coverage in x64.
- User retest on repeated highlight switching between different nodes.

## Implementation Summary
- Captured the previous highlighted handle before switching proxies.
- Requested redraws for the previous and current target roots both before and after the overlay move.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt22` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt22\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 4/4 tests.

## Outcome
Highlight switches now invalidate the stale old-frame region after the overlay has moved, which should give the target app the same repaint opportunity that the user's manual click was previously providing.

## Follow-up Update
- 2026-04-27: user reported no visible change after this attempt.
- Superseded by `fix-attempt-023.md`, which recreates the persistent overlay window on target switches instead of reusing the same transparent form instance.

## Next Step
User confirmation on whether the old frame finally disappears immediately on repeated highlight switches.

## Remaining Gaps
- Behavioral confirmation in the user's application set is still pending.
