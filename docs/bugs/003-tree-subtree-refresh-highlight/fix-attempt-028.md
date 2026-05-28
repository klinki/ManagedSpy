# Fix Attempt 028

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Restore the raw-window DPI fallback for "Keep Highlighted" rectangles that are still clear DPI-scaled twins of the raw target rectangle, even when the oversized scaled rectangle overlaps a large portion of the target root window.

## Relation To Previous Attempts
Follow-up to `fix-attempt-027.md`. Attempt 027 fixed the logged eM Client cases by falling back to the raw Win32 rectangle when the managed rectangle was a near-uniform DPI-scaled version of it and the raw rectangle had a larger root-window intersection. The new `screenshots\keep_highlighted_invalid_location.png` regression shows the same scaled-twin shape again, but the oversized rectangle can overlap enough of the root window that a plain intersection-area comparison no longer proves the raw rectangle is better.

## Proposed Change
- Keep the existing scale-signature guard so raw-window fallback only applies when the managed rectangle is a near-uniform DPI-scaled twin of the raw rectangle.
- Improve the root-window fit check to also compare the percentage of each rectangle that remains inside the root window.
- Fall back to the raw rectangle when the raw rectangle is substantially better contained by the root, even if the oversized scaled rectangle has a larger absolute intersection area.
- Add a deterministic regression test for the new screenshot pattern.

## Risks
- The fallback must stay conservative so valid managed child rectangles are not replaced by ancestor/raw HWND rectangles.
- The affected eM Client scenario is not locally reproducible, so user confirmation remains required after local validation.

## Files And Components
- `ManagedSpyLib\ScreenBoundsHelper.cs`
- `ManagedSpy.Tests\ScreenBoundsHelperTests.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`
- `docs\bugs\003-tree-subtree-refresh-highlight\description.md`
- `docs\bugs\003-tree-subtree-refresh-highlight\initial-findings.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64` into an isolated output root.
- Run `ManagedSpy.Tests.dll` from both built outputs with the matching `/Platform` value.
- Ask the user to retest the eM Client "Keep Highlighted" scenario shown in `screenshots\keep_highlighted_invalid_location.png`.

## Implementation Summary
- Added a root-window containment coverage check to `ScreenBoundsHelper.ShouldUseRawWindowDpiFallback`.
- Kept the existing near-uniform DPI-scale signature check as the gate before the fallback can run.
- Preserved the previous absolute intersection-area fallback for earlier logged cases.
- Added a regression test where the scaled candidate has a larger absolute overlap with the root, but the raw rectangle is substantially better contained.

## Test Results
- Baseline before changes:
  - `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\baseline-build` succeeded for Release x86/x64.
  - `dotnet vstest ...\baseline-build\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 13/13 tests.
  - `dotnet vstest ...\baseline-build\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 13/13 tests.
- After changes:
  - `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt28-build` succeeded for Release x86/x64.
  - `dotnet vstest ...\attempt28-build\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 14/14 tests.
  - Initial `dotnet vstest ...\attempt28-build\x64\ManagedSpy.Tests.dll /Platform:x64` run had one UIAutomation startup timeout in `ManagedSpy_InspectsManagedWinFormsTarget_WithUiAutomation`; rerun passed 14/14 tests.

## Outcome
- The raw-window DPI fallback now handles scaled candidates that spill outside the root even when their absolute root overlap is larger than the raw target rectangle's overlap.
- Local build and automated regression coverage are complete.

## Follow-up Update
- 2026-05-27: user reported the issue is still broken after this attempt.
- New evidence in `screenshots\highlight_login_button.png` shows `button_EmSync_Login` selected, while the highlight appears near the lower-right of the Settings window instead of on the visible **Log in** button.
- The user also reported that the magnifier path works correctly and requested a new research-first bug report named `keep-hignlighted-position`.
- Superseded by `docs\bugs\005-keep-hignlighted-position\`.

## Next Step
- Continue investigation in `docs\bugs\005-keep-hignlighted-position\`.

## Remaining Gaps
- Direct reproduction in eM Client is not available in this environment.
