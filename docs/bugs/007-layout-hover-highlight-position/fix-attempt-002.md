# Fix Attempt 002

## Attempt Status
fixed

## Goal
Correct the remaining Layout tab hover offset after attempt 001 still highlighted away from the real target area.

## Relation To Previous Attempts
Attempt 001 moved layout rectangles through target-side screen normalization and clipping, but the user reported the Layout hover highlight was still broken. This attempt keeps those normalized layout measurements but anchors the overlay placement to the same resolved element rectangle used by the working persistent-highlight path.

## Proposed Change
- Resolve the selected control's element rectangle through `TryGetPersistentHighlightRectangle`.
- Return that resolved rectangle directly for the Layout **Element** section.
- Map **Margin**, **Padding**, and **Content** rectangles relative to `ControlLayoutInfo.BoundsScreen` onto the resolved element rectangle, preserving box-model offsets while avoiding a second incompatible coordinate conversion.
- Add a regression test for the anchor-based mapping.

## Risks
- If a layout section was independently clipped differently from the element rectangle, relative mapping can slightly compress that section.
- The fallback path must remain available if the persistent-highlight rectangle cannot be resolved.
- The change should not alter persistent highlighting itself.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `ManagedSpy.Tests\ControlLayoutInfoTests.cs`
- `docs\bugs\007-layout-hover-highlight-position\*`

## Verification Plan
- Build with `.\build.ps1`.
- Run x64 artifact tests.
- Run x86 artifact tests.
- Ask the user to retest Layout hover highlights in the target application.

## Implementation Summary
- Updated `TryGetLayoutHighlightRectangle` to resolve the selected control through the persistent-highlight rectangle path first.
- Added `MapLayoutSectionToOverlayRectangle` so non-element Layout sections are translated from layout metadata space onto the resolved overlay element rectangle.
- Kept the previous normalization fallback for cases where the selected control cannot provide a persistent-highlight rectangle.
- Added `LayoutHighlightMapping_AnchorsSectionToResolvedElementRectangle`.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\refresh-layout-build-final` passed.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\refresh-layout-build-final\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 17/17.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\refresh-layout-build-final\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 17/17.

## Outcome
- Local implementation and automated validation completed successfully.
- User confirmed Layout hover highlights line up with the real target area on 2026-05-29.

## Next Step
- Ask the user to retest the Layout tab hover highlight.

## Remaining Gaps
- None for the original hover-location symptom.
