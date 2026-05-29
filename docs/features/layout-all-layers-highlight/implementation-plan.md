# Layout All Layers Highlight Implementation Plan

## Feature Slug
layout-all-layers-highlight

## Target Documentation Path
[docs/features/layout-all-layers-highlight/implementation-plan.md](implementation-plan.md)

## Artifact Type
implementation-plan.md

## Goal
Add a **Highlight all layers** option to the Layout tab so users can show every layout layer outline on the target at the same time instead of hovering one section at a time.

## Scope
- Add a checkbox to the Layout tab named **Highlight all layers**.
- Keep the existing hover-to-highlight behavior when the checkbox is unchecked.
- When checked, show margin, element, padding, and content overlays together using the same colors as the Layout diagram.
- Refresh all layer overlays while the checkbox is checked so target movement stays aligned.
- Hide all layer overlays when the checkbox is unchecked, the Layout tab is not active, or the current selection has no layout data.

## User Workflow
1. Select a managed control in the inspection tree.
2. Open the **Layout** tab.
3. Hover a single layout section to highlight only that section, or check **Highlight all layers** to show every layer outline at once.
4. Uncheck **Highlight all layers** to return to hover-only highlighting.

## Technical Approach
1. Wrap the Layout tab contents in a table layout with the checkbox above the existing diagram.
2. Keep the existing single hover overlay for unchecked behavior.
3. Add per-layer overlay forms keyed by `LayoutSection` for all-layer highlighting.
4. Reuse the existing layout rectangle resolution path so all-layer overlays get the same DPI and target-bound fixes as hover highlights.
5. Update all layer overlays on a timer while the checkbox is checked, and stop the timer when highlighting is not applicable.
6. Dispose the additional overlay forms during main form shutdown.

## Files And Components
- [ManagedSpy/MainForm.cs](../../../ManagedSpy/MainForm.cs)
- [ManagedSpy/MainForm.Designer.cs](../../../ManagedSpy/MainForm.Designer.cs)
- [ManagedSpy.Tests/ManagedSpyEndToEndTests.cs](../../../ManagedSpy.Tests/ManagedSpyEndToEndTests.cs)
- [docs/specification/features-and-workflows.md](../../specification/features-and-workflows.md)
- [docs/specification/usage-manual.md](../../specification/usage-manual.md)

## Validation
- Build the repository with `.\build.ps1`.
- Run x64 artifact tests with `dotnet vstest <output>\x64\ManagedSpy.Tests.dll /Platform:x64`.
- Run x86 artifact tests with `dotnet vstest <output>\x86\ManagedSpy.Tests.dll /Platform:x86`.
- Manually confirm that checking **Highlight all layers** shows all four layer outlines and unchecking it restores hover-only highlighting.

## Risks
- Multiple Layout overlays can overlap; use the existing section colors so users can still distinguish each layer.
- Timer-based updates should stay limited to the checked state to avoid adding background UI work during normal inspection.
