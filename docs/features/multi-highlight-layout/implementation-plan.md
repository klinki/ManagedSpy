# Multi Highlight And Layout Tab Implementation Plan

## Feature Slug
multi-highlight-layout

## Target Documentation Path
`docs\features\multi-highlight-layout\implementation-plan.md`

## Artifact Type
implementation-plan.md

## Goal
Add multi-target persistent highlighting and a Layout tab that exposes selected-control box-model details with hover-driven screen highlights.

## Scope
- Allow multiple tree items to stay highlighted at the same time.
- Assign each newly highlighted item a distinct palette color.
- Preserve existing persistent-highlight diagnostics, DPI normalization, and target lifecycle cleanup.
- Add a `Layout` tab next to `Events`.
- Show margin, padding, main element size, and content/display size for the selected managed control.
- Highlight the corresponding target rectangle when hovering layout sections.
- Match the screen highlight color to the hovered Layout layer color.
- Put layer names and measurements directly inside the interactive Layout diagram.

## Technical Approach
1. Replace the single persistent highlight target in `ManagedSpy\MainForm.cs` with a collection of persistent highlight entries.
2. Store each entry's `ControlProxy`, last rectangle, overlay form, and assigned color.
3. Re-key entries explicitly when `ControlProxy.HandleChanged` fires so HWND recreation does not leave stale dictionary keys.
4. Dispose overlay forms whenever entries are removed because of user toggles, window destruction, process exit, refresh, or form close.
5. Extend `HighlightOverlayForm` to accept a border color while keeping the existing red default for transient flashes.
6. Add serializable layout metadata in `ManagedSpyLib` and a cross-process `GetManagedLayout` message.
7. Compute target layout rectangles in the target process using WinForms semantics:
   - margin rectangle: element bounds inflated by `Control.Margin`
   - element rectangle: control bounds on screen
   - padding/client rectangle: client rectangle on screen
   - content rectangle: `Control.DisplayRectangle` on screen
8. Cache selected layout metadata in `MainForm`; do not re-query the target on each mouse move.
9. Add a custom `LayoutViewControl` to render nested sections and raise hover-section changes.
10. Use a dedicated layout-hover overlay and hide it on mouse leave, selection changes, tab changes, and form deactivation.
11. Expose the Layout view's per-section accent colors so the screen overlay uses the same layer color as the diagram.
12. Render layer labels and measurements on top of the diagram itself instead of relying on a separate details block.
13. Keep edge measurements compact in the diagram: show only numbers on edges, avoid duplicate full `T/R/B/L` summaries, and center the content label inside the content box.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `ManagedSpy\MainForm.Designer.cs`
- `ManagedSpyLib\ControlLayoutInfo.cs`
- `ManagedSpyLib\ControlProxy.cs`
- `ManagedSpyLib\Desktop.cs`
- `ManagedSpyLib\ManagedSpyMessages.cs`
- `ManagedSpy.Tests\ControlLayoutInfoTests.cs`
- `ManagedSpy.Tests\ManagedSpyEndToEndTests.cs`

## Validation
- Build the repository with `.\build.ps1`.
- Run x64 artifact tests with `dotnet vstest <output>\x64\ManagedSpy.Tests.dll /Platform:x64`.
- Run x86 artifact tests with `dotnet vstest <output>\x86\ManagedSpy.Tests.dll /Platform:x86`.

## Risks
- Multiple pinned targets increase synchronous cross-process work on the UI timer; keep per-target cleanup isolated and preserve diagnostics.
- Box-model geometry for WinForms controls is not identical to browser CSS; use `DisplayRectangle` and documented WinForms properties rather than CSS assumptions.
- Overlay z-order and target redraw behavior must preserve the recent persistent-highlight lifecycle fixes.
