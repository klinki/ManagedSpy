# Fix Attempt 025

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Stop the shared DPI-normalization step from double-scaling native client rectangles that are already in usable screen coordinates.

## Relation To Previous Attempts
Follow-up to `fix-attempt-024.md`. The new eM Client log showed the same 1.75x scale-up on both `preferred` and `nonAccessible` rectangles, which means the bug is happening after accessibility selection and inside the shared normalization path.

## Proposed Change
- Keep DPI normalization for accessibility-derived bounds, because that path previously improved eM Client and ManagedSpy.
- Stop applying that same normalization to native client bounds returned by `TryGetClientScreenBounds` / `RectangleToScreen`.
- Keep the attempt 024 plausibility guard so that if accessibility bounds are still detached, the non-normalized native client path wins.

## Risks
- Some target apps may still need DPI normalization on a non-accessibility path, so removing it from the native client branch could reintroduce old geometry issues elsewhere.
- This change still relies on user retesting because the affected app is not available locally.

## Files And Components
- `ManagedSpyLib\ScreenBoundsHelper.cpp`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64` into an isolated output root.
- Run the existing `ManagedSpy.Tests.dll` tests from the built output.
- User retest in eM Client with the scenario captured in `screenshots\bug_dpi_02.png`.

## Implementation Summary
- Kept DPI normalization on the accessibility-derived path.
- Stopped applying that same normalization to native client rectangles returned by the non-accessibility path.
- Preserved the attempt 024 plausibility guard so detached accessibility rectangles can still fall back to the native client path.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\5361b3fb-b28f-428a-8c27-aa25cfafd720\files\build-out-attempt25` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\5361b3fb-b28f-428a-8c27-aa25cfafd720\files\build-out-attempt25\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 5/5 tests.
- `dotnet vstest C:\Users\david\.copilot\session-state\5361b3fb-b28f-428a-8c27-aa25cfafd720\files\build-out-attempt25\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 5/5 tests.

## Outcome
- Native client rectangles no longer get pushed through the shared DPI-normalization step that was over-scaling them by exactly 1.75x in the user's diagnostics.

## Follow-up Update
- 2026-04-28: user reported the offset was still present in eM Client and shared `screenshots\bug_dpi_03.png`.
- The accompanying diagnostic log still showed the same 1.75x overshoot on both `preferred` and `nonAccessible` rectangles, so changing target-process normalization alone was insufficient.
- Superseded by `fix-attempt-026.md`, which targets the local overlay coordinate space in ManagedSpy rather than the target-process helper alone.

## Next Step
- User retest in eM Client with the scenario captured in `screenshots\bug_dpi_02.png`.

## Remaining Gaps
- Direct reproduction in eM Client is still not available in this environment, so user confirmation is still required.
