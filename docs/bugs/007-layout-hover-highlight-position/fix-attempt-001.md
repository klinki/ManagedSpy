# Fix Attempt 001

## Attempt Status
awaiting-user-confirmation

## Goal
Make Layout tab hover highlights use the correct on-screen rectangle for the selected target area.

## Relation To Previous Attempts
First attempt for this bug.

## Proposed Change
- Generate layout rectangles through `ScreenBoundsHelper` so they use the same managed screen-coordinate normalization approach as Keep Highlighted.
- Keep the ManagedSpy-side hover pipeline unchanged except for consuming corrected target-side layout rectangles.
- Add regression coverage so layout geometry stays aligned with the existing screen-bounds helper path.

## Risks
- Margin rectangles are parent-relative layout metadata, so they must be converted in the correct coordinate space before normalization.
- Layout rectangle changes should not break the new Layout tab or existing artifact tests.
- Nested controls must also be clipped to visible ancestor client bounds or the Layout tab can still highlight invisible areas outside the parent.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `ManagedSpyLib\ControlLayoutInfo.cs`
- `ManagedSpyLib\ScreenBoundsHelper.cs`
- `ManagedSpy.Tests\ControlLayoutInfoTests.cs`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64` into an isolated output root.
- Run `ManagedSpy.Tests.dll` from both built outputs with matching `/Platform` values.
- Ask the user to retest the Layout tab hover highlight in their target application.

## Implementation Summary
- Added `ScreenBoundsHelper.GetRelativeScreenBounds` to normalize target-side relative rectangles and clip them against the coordinate control and ancestor client areas.
- Updated `ControlLayoutInfo.FromControl` to build element, margin, client, and content rectangles through `ScreenBoundsHelper` instead of using raw `RectangleToScreen` output directly.
- Kept the existing `MainForm` hover highlight pipeline intact so the Layout tab now feeds it corrected target-side rectangles rather than layering a second workaround on top.
- Replaced the self-referential layout regression assertions with independent expected geometry and added a clipped nested-control test.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\layout-hover-highlight-build-2` passed.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\layout-hover-highlight-build-2\x64\ManagedSpy.Tests.dll /Platform:x64 --logger:"console;verbosity=minimal"` passed: 16/16.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\layout-hover-highlight-build-2\x86\ManagedSpy.Tests.dll /Platform:x86 --logger:"console;verbosity=minimal"` passed: 16/16.

## Outcome
- Local implementation and automated validation completed successfully.
- Bug remains open until the user confirms the Layout tab highlight matches the real target area in their environment.

## Next Step
- Ask the user to retest the Layout tab hover highlight.

## Remaining Gaps
- User-environment confirmation is still needed for the original hover-location symptom.
