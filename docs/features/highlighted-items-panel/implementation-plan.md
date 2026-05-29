# Highlighted Items Panel Implementation Plan

## Feature Slug
highlighted-items-panel

## Target Documentation Path
[docs/features/highlighted-items-panel/implementation-plan.md](implementation-plan.md)

## Artifact Type
implementation-plan.md

## Goal
Add a left-side **Highlighted Items** section that lets users review, select, and clear persistent highlight targets without hunting through the inspection tree.

## Scope
- Add a section below the process tree named **Highlighted Items**.
- Show one row for each active persistent highlight.
- Render each highlight row with the target color swatch and the same label used by the tree view.
- Select the corresponding tree item and show its properties when a row is clicked.
- Add a **Remove All Highlights** button.
- Keep the existing tree context menu workflow for toggling individual persistent highlights.

## User Workflow
1. Right-click a tree item and choose **Keep Highlighted**.
2. The target remains highlighted on screen and appears in **Highlighted Items**.
3. Click a highlighted item row to select the matching tree item and show its properties.
4. Use **Remove All Highlights** to clear every active persistent highlight and hide their overlays.

## Technical Approach
1. Wrap the existing left tree view in a horizontal split container.
2. Put the tree in the top pane and the new **Highlighted Items** grid in the bottom pane.
3. Store a stable display label on each persistent highlight target when it is created.
4. Rebuild the grid when highlights are added, removed, cleared, or re-keyed after handle recreation.
5. Paint the color column as a swatch rectangle using the target's assigned persistent-highlight color.
6. Store each target handle in the row tag so row clicks can resolve the active `PersistentHighlightTarget`.
7. Reuse the existing finder path selection method to materialize/select the matching tree node when a highlighted row is clicked.
8. Keep grid updates safe when targets disappear by falling back to the cached label and handle.

## Files And Components
- [ManagedSpy/MainForm.cs](../../../ManagedSpy/MainForm.cs)
- [ManagedSpy/MainForm.Designer.cs](../../../ManagedSpy/MainForm.Designer.cs)
- [docs/specification/features-and-workflows.md](../../specification/features-and-workflows.md)
- [docs/specification/usage-manual.md](../../specification/usage-manual.md)

## Validation
- Build the repository with `.\build.ps1`.
- Run x64 artifact tests with `dotnet vstest <output>\x64\ManagedSpy.Tests.dll /Platform:x64`.
- Run x86 artifact tests with `dotnet vstest <output>\x86\ManagedSpy.Tests.dll /Platform:x86`.
- Manually confirm that adding, selecting, and clearing persistent highlights updates the new panel.

## Risks
- Clicking a highlighted item can still fail if the target process has exited before the row is selected.
- A target can recreate its HWND while highlighted; the grid must refresh when highlight entries are re-keyed.
- Adding a bottom section reduces tree height, so the left split should preserve reasonable minimum sizes.
