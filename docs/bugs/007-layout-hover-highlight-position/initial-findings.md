# Initial Findings

## Confirmed Facts
- `ControlLayoutInfo.FromControl` builds layout rectangles with `RectangleToScreen` and `control.Bounds`.
- `MainForm.TryGetLayoutHighlightRectangle` then passes those rectangles through `NormalizeRectangleToLocalCoordinates`.
- Persistent highlight geometry already uses a target-side helper path (`GetScreenBounds` / `ScreenBoundsHelper`) that was hardened for DPI-related mismatches.

## Likely Cause
- Layout rectangles are being produced in a different coordinate space than the persistent highlight path.
- The ManagedSpy-side normalization step appears to be correcting rectangles that were not prepared the same way as `GetScreenBounds`, which can produce the same kind of misalignment seen in the earlier Keep Highlighted bug.

## Unknowns
- Whether the current bug affects every layout section equally or primarily the element/client/content rectangles.
- Whether the user's target app is exposing logical rather than physical coordinates from `RectangleToScreen` under DPI scaling.

## Reproduction Status
- Direct reproduction with the user's target app is not available in this environment.
- Code inspection strongly suggests a coordinate-space mismatch between target-side layout rectangle generation and the existing overlay highlight pipeline.

## Evidence Gathered
- `ManagedSpy\MainForm.cs`
- `ManagedSpyLib\ControlLayoutInfo.cs`
- `ManagedSpyLib\ScreenBoundsHelper.cs`

## Update 2026-05-28
- A focused review found that layout rectangles also need ancestor client clipping, not just DPI normalization, or nested controls can still highlight invisible areas outside their parent.
- The original Layout regression test was self-referential because it computed expected values through the same helper as production. Independent clipped-control expectations were added so the test now catches this class of bug.
