# Bug Description

## Title
Element finder highlights on wrong screen and leaves visual artifacts after disable

## Status
- fixed

## Reported Symptoms
- Finder highlight appears on a different monitor than the hovered element.
- Disabling finder leaves screen artifacts.
- The current highlight behavior appears to redraw parts of the screen instead of only the outline rectangle.

## Expected Behavior
- Highlight should be drawn exactly around the currently hovered element on the correct monitor.
- Stopping finder should remove all highlight visuals cleanly.

## Actual Behavior
- Highlight coordinates are sometimes incorrect in multi-monitor/high-DPI setups.
- Reversible frame drawing sometimes leaves stale XOR artifacts.

## Reproduction Details
1. Start ManagedSpy.
2. Enable element finder.
3. Hover elements on different monitors/scaled displays.
4. Disable finder and observe leftover artifacts.

## Affected Area
- `ManagedSpy\MainForm.cs`
  - element finder highlighting (`UpdateFinderHighlight`, `RemoveFinderHighlight`)
  - cursor/point detection (`GetWindowHandleAtCursor`)
  - flash behavior (`FlashWindowHandle`)

## Constraints
- Keep existing element-pick workflow and tree/property activation behavior.
- Avoid introducing focus stealing while highlighting.

## Open Questions
- Whether some environments require additional DPI-awareness adjustments after switching to overlay-based rendering.
