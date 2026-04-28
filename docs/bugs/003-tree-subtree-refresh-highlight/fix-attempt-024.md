# Fix Attempt 024

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Prevent first-selection persistent highlighting from trusting accessibility-derived bounds when they no longer plausibly line up with the selected control's native client rectangle in eM Client.

## Relation To Previous Attempts
Follow-up to `fix-attempt-023.md`. Attempt 023 fixed stale overlay reuse on target switches, but the new eM Client screenshot points back to a remaining geometry problem rather than the old rendering-lag issue. This attempt also builds on attempts 012 and 014 by revisiting the accessibility-preferred path that can still be wrong for some controls even after DPI normalization.

## Proposed Change
- In `ScreenBoundsHelper`, compute both accessibility-derived and native client bounds when accessibility is preferred.
- Keep accessibility bounds only when they still plausibly describe the same on-screen region as the control's native client rectangle.
- Fall back to the native client path when the accessibility rectangle is detached from the control's actual HWND/client geometry.
- Add a regression test that proves clearly implausible accessibility bounds fall back to the native client rectangle.

## Risks
- Some controls intentionally expose a smaller accessibility region than their full client bounds, so the plausibility guard must preserve those valid cases.
- If both accessibility and native bounds are wrong in the same way for a target app, this attempt will not fully solve the issue.

## Files And Components
- `ManagedSpyLib\ScreenBoundsHelper.cpp`
- `ManagedSpy.Tests\ScreenBoundsHelperTests.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64` into an isolated output root.
- Run the existing `ManagedSpy.Tests.dll` tests from the built output.
- User retest in eM Client on the same label/highlight scenario shown in `screenshots\bug_dpi_issues.png`.

## Implementation Summary
- Split child-control bounds resolution into separate accessibility-derived and native client candidates.
- Added a plausibility guard that keeps accessibility bounds only when they still overlap the control's native client rectangle strongly enough to describe the same on-screen control.
- Fell back to the native client path when the accessibility rectangle is detached from the control's actual geometry.
- Added a regression test covering clearly implausible accessibility bounds.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\5361b3fb-b28f-428a-8c27-aa25cfafd720\files\build-out-attempt24` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\5361b3fb-b28f-428a-8c27-aa25cfafd720\files\build-out-attempt24\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 5/5 tests.
- `dotnet vstest C:\Users\david\.copilot\session-state\5361b3fb-b28f-428a-8c27-aa25cfafd720\files\build-out-attempt24\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 5/5 tests.

## Outcome
- Persistent highlight no longer blindly trusts clearly detached accessibility bounds on first selection; it now falls back to the selected control's native client geometry when the accessibility rectangle does not plausibly match the same control.

## Follow-up Update
- 2026-04-28: user reported the offset was still present in eM Client and shared `screenshots\bug_dpi_02.png`.
- The accompanying diagnostic log showed the same 1.75x scaling on both `preferred` and `nonAccessible` rectangles, so this attempt did not address the real root cause.
- Superseded by `fix-attempt-025.md`, which targets the shared DPI-normalization step instead of accessibility selection alone.

## Next Step
- User retest in eM Client on the same scenario shown in `screenshots\bug_dpi_issues.png`.

## Remaining Gaps
- Direct reproduction in eM Client is still not available in this environment, so user confirmation is still required.
