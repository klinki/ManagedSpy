# Bug Description

## Title
Layout tab hover highlight is drawn at the wrong screen location

## Status
- fixed

## Reported Symptoms
- Hovering sections in the new **Layout** tab highlights a rectangle far away from the real target area.
- The behavior resembles the earlier **Keep Highlighted** coordinate-space bug.
- After attempt 001, the user reported the Layout highlight was still broken and still highlighted very far from the real location.

## Expected Behavior
- Hovering **Margin**, **Element**, **Padding**, or **Content** in the Layout tab should highlight the corresponding target area on screen.
- Layout hover highlights should use the same reliable screen-coordinate handling as persistent highlight overlays.

## Actual Behavior
- Layout hover rectangles are offset far from the target control.
- The layout model currently builds screen rectangles directly from `RectangleToScreen`, then the ManagedSpy side applies the same normalization step used for persistent highlight rectangles.
- Attempt 001 improved the target-side rectangle generation, but user retesting showed the overlay placement still did not match the real target location.
- Attempt 002 anchored the Layout overlay to the persistent-highlight rectangle path, and the user confirmed the Layout highlight works on 2026-05-29.

## Reproduction Details
1. Launch a compatible target app.
2. Launch the matching ManagedSpy build.
3. Select a managed control.
4. Open the **Layout** tab.
5. Hover a layout section.
6. Observe the overlay highlight drawn far from the real control area.

## Affected Area
- `ManagedSpy\MainForm.cs`
  - layout hover highlight rectangle selection
- `ManagedSpyLib\ControlLayoutInfo.cs`
  - layout rectangle generation
- `ManagedSpyLib\ScreenBoundsHelper.cs`
  - managed screen-coordinate normalization

## Constraints
- Preserve the existing Keep Highlighted lifecycle and DPI fixes.
- Keep layout hover using cached layout data rather than re-querying the target on every mouse move.
- Do not break the new Layout tab surface or existing artifact tests.

## Open Questions
- Whether all layout rectangles should be generated through the same helper path as `GetControlScreenBounds`.
- Whether margin bounds need parent-relative conversion before screen normalization for scaled targets.
