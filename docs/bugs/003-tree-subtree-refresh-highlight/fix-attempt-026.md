# Fix Attempt 026

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Convert target-process rectangles into ManagedSpy's local overlay coordinate space before comparing or drawing them.

## Relation To Previous Attempts
Follow-up to `fix-attempt-025.md`. Removing target-process normalization from the native branch did not change the user's result, which suggests the remaining mismatch is no longer in the target-process helper itself. The repeated 1.75x ratio now points to a cross-process DPI-awareness mismatch between eM Client and ManagedSpy's overlay process.

## Proposed Change
- Add a local rectangle-normalization step in `ManagedSpy\MainForm.cs` for rectangles returned by `proxy.GetScreenBounds(...)`.
- Convert those rectangles into the coordinate space used by ManagedSpy before comparing them to locally collected raw/root rectangles or passing them to the overlay window.
- Use a fit-to-root heuristic so the conversion only wins when it better matches the locally observed target window bounds.

## Risks
- Coordinate conversion APIs can be subtle across monitors and DPI contexts, so the local-space heuristic needs to stay conservative.
- If the right long-term fix is making ManagedSpy explicitly DPI-aware, this attempt may still only be an intermediate workaround.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64` into an isolated output root.
- Run the existing `ManagedSpy.Tests.dll` tests from the built output.
- User retest in eM Client with the scenario captured in `screenshots\bug_dpi_03.png`.

## Implementation Summary
- Added local-space normalization in `MainForm` for rectangles returned by `proxy.GetScreenBounds(...)`.
- Converted target-process rectangles through `PhysicalToLogicalPointForPerMonitorDPI` using ManagedSpy's local window context.
- Applied a fit-to-root heuristic so the local conversion only wins when it better matches the locally observed root-window bounds.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\5361b3fb-b28f-428a-8c27-aa25cfafd720\files\build-out-attempt26` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\5361b3fb-b28f-428a-8c27-aa25cfafd720\files\build-out-attempt26\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 5/5 tests.
- `dotnet vstest C:\Users\david\.copilot\session-state\5361b3fb-b28f-428a-8c27-aa25cfafd720\files\build-out-attempt26\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 5/5 tests.

## Outcome
- ManagedSpy now has a local overlay-space normalization step for target-process rectangles, which should collapse the repeated 1.75x overshoot back into the coordinate space used by the overlay window.

## Follow-up Update
- 2026-04-28: user reported no visible change after this attempt and did not need a new screenshot because the behavior was unchanged.
- The accompanying diagnostic log still showed the same 1.75x overshoot, which means this API-based local conversion did not affect the rectangles as expected.
- Superseded by `fix-attempt-027.md`, which uses the already-correct local raw Win32 rectangle as a conservative fallback signal instead.

## Next Step
- User retest in eM Client with the scenario captured in `screenshots\bug_dpi_03.png`.

## Remaining Gaps
- Direct reproduction in eM Client is still not available in this environment, so user confirmation is still required.
