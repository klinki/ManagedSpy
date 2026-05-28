# Keep Highlighted Position Research

## GPT

### Observed behavior
- The current failure is not just an oversized rectangle. `highlight_login_button.png` shows a relatively small highlight around the lower-right portion of the eM Client Settings window while `button_EmSync_Login` is selected in the ManagedSpy tree.
- The property grid shows the selected button size as `74; 41`, but the overlay appears far from the visible **Log in** button. That makes a pure "wrong size" heuristic insufficient.
- The earlier `keep_highlighted_invalid_location.png` failure shows a huge rectangle lower on the window. That still looks like an ancestor/container or DPI-scaled rectangle being chosen.
- The user reports the magnifier path works correctly. That matters because the magnifier path and persistent path do not use the same geometry pipeline.

### Code path comparison
- The magnifier path in `ManagedSpy\MainForm.cs` uses cursor hit-testing and direct local HWND geometry:
  - `GetWindowHandleAtCursor()`
  - `UpdateFinderHighlight(IntPtr windowHandle)`
  - `TryGetWindowRectangle(windowHandle, out rectangle)`
  - `highlightOverlay.ShowHighlight(rectangle)`
- The persistent path uses tree selection and cross-process managed geometry:
  - `EnablePersistentHighlight(ControlProxy proxy)`
  - `UpdatePersistentHighlight(...)`
  - `TryGetPersistentHighlightRectangle(...)`
  - `proxy.GetScreenBounds()`
  - `proxy.GetScreenBounds(false)` only when a previous rectangle exists
  - `NormalizeRectangleToLocalCoordinates(...)`
  - `ResolveRawWindowDpiFallback(...)`
- `ControlProxy.GetScreenBounds()` sends `ManagedSpyMessages.GetManagedScreenRect` to the target process. The target resolves `managedChildPath` back to a WinForms `Control` and calls `ScreenBoundsHelper.GetControlScreenBounds`.
- `ScreenBoundsHelper.GetControlScreenBounds` can use accessibility bounds, native client bounds, ancestor clipping, and DPI normalization inside the target process.

### Main hypotheses
1. **Mixed coordinate spaces are the primary cause.** eM Client likely reports target-process rectangles in a DPI-aware physical coordinate space, while ManagedSpy's overlay is positioned using the local process coordinate space. ManagedSpy has no explicit DPI-awareness declaration, so local `GetWindowRect` calls may be virtualized.
2. **The existing local normalization may be ineffective.** `PhysicalToLogicalPointForPerMonitorDPI(this.Handle, ...)` uses ManagedSpy's own window as the reference. If ManagedSpy is DPI-unaware, this conversion may not produce the coordinate-space conversion needed for eM Client rectangles.
3. **Raw fallback can still compare different visual elements.** For child controls, `proxy.Handle` may represent either the selected control's own HWND or a containing ancestor HWND. Falling back to `GetWindowRect(proxy.Handle)` is only safe when that HWND represents the same visual element as the selected tree node.
4. **The persistent path lacks enough context in diagnostics.** Current diagnostics log preferred, non-accessible, raw, root, chosen, and source, but not enough to prove whether the raw HWND is the selected control, whether path resolution succeeded, or what DPI context each source used.

### Debug information to add before the next fix
- For every persistent-highlight decision, log:
  - target process id and ManagedSpy process id
  - selected component name and class name
  - selected proxy handle
  - parent handle and root handle
  - raw window rectangle for proxy handle
  - root window rectangle
  - preferred target-process rectangle before local normalization
  - preferred rectangle after local normalization
  - non-accessible target-process rectangle before local normalization
  - non-accessible rectangle after local normalization
  - chosen rectangle and chosen source
  - whether raw fallback was considered and why it passed or failed
  - `GetDpiForWindow` for ManagedSpy, proxy handle, and root handle
  - whether `PhysicalToLogicalPointForPerMonitorDPI` changed either point
  - coverage/intersection/scale values used by `ShouldUseRawWindowDpiFallback`
- Add a target-process debug payload if practical:
  - resolved control type
  - resolved control name
  - `managedChildPath` length
  - control `Bounds`, `ClientRectangle`, `Size`
  - `RectangleToScreen(ClientRectangle)`
  - accessibility bounds
  - native client screen bounds
  - ancestor clip rectangles
  - target-side `GetDpiForWindow` for the resolved control/root

### Proposed implementation direction after diagnostics
1. Add instrumentation first and ask for a diagnostic log from the failing eM Client cases.
2. Use the log to classify each failure:
   - target-process rectangle correct but local normalization wrong
   - target-process rectangle already wrong
   - raw fallback incorrectly selected an ancestor/container
   - selected path resolves to a stale/wrong target
3. Fix selection logic based on classification:
   - prefer local raw `GetWindowRect(proxy.Handle)` only when evidence shows `proxy.Handle` is the selected control's own HWND
   - keep target-process bounds for windowless child controls
   - avoid raw fallback when raw size/area/position proves it is an ancestor rather than the selected child
   - replace local DPI conversion with a deterministic conversion strategy only after logs prove the source/target coordinate spaces

## Opus

### Key facts from the independent review
- The magnifier/finder path calls `GetWindowRect` directly on the HWND under the cursor and works because it uses local Win32 coordinates.
- The persistent highlight path uses `proxy.GetScreenBounds()`, which marshals a request into the target process and executes `ScreenBoundsHelper.GetControlScreenBounds`.
- `proxy.Handle` can point to a nearest HWND ancestor instead of the exact managed child control when the selected tree item is represented by `managedChildPath`.
- `ScreenBoundsHelper.GetControlScreenBounds` returns the resolved child's bounds, but `TryGetPersistentHighlightRectangle` compares those bounds against `GetWindowRect(proxy.Handle)`.
- ManagedSpy has no DPI-awareness manifest/config, while eM Client is likely per-monitor DPI-aware.

### Hypotheses ranked by the independent review
1. **High likelihood:** raw-window DPI fallback compares child-control bounds against an ancestor HWND rectangle and can create false-positive scale-signature matches.
2. **Medium-high likelihood:** `NormalizeRectangleToLocalCoordinates` corrupts otherwise valid target-process coordinates because it uses a DPI-unaware ManagedSpy reference handle.
3. **Medium likelihood:** target-process bounds and local raw HWND bounds are in different coordinate spaces because Windows virtualizes coordinates for DPI-unaware ManagedSpy but not for the DPI-aware target process.
4. **Lower likelihood:** cached `managedChildPath` can become stale if eM Client dynamically changes the control hierarchy.

### Debug instrumentation recommended by the independent review
- Log whether `proxy.Handle` is a top-level/root handle, child HWND, or likely ancestor/container.
- Log target control type and managed path length.
- Log `GetDpiForWindow(proxy.Handle)`, `GetDpiForWindow(this.Handle)`, and process DPI awareness where possible.
- Log whether `GetWindowRect(proxy.Handle)` matches `GetWindowRect(GetAncestor(proxy.Handle, GA_ROOT))`.
- Log the same local raw rectangle that the magnifier-style path would use for the selected proxy handle.
- Log whether raw rectangle size is comparable to the target-process control size before allowing raw fallback.

### Implementation plan recommended by the independent review
- First add diagnostics that discriminate whether the failure is a bad target-process rectangle, bad local conversion, bad raw fallback, or stale path resolution.
- Prefer local `GetWindowRect(proxy.Handle)` only when the selected control owns that HWND or the raw rectangle is comparable to the selected control's expected size.
- Keep cross-process managed bounds for controls without their own HWND.
- Do not rely solely on scale-signature matching; ensure the compared rectangles describe the same visual element.
- Validate with existing automated tests plus user retests for both `button_EmSync_Login` and `optionButton_CustomSetup...`.

## Summary

### Current best explanation
`Keep Highlighted` is still wrong because the persistent path combines target-process child-control bounds, local raw HWND rectangles, and DPI conversion heuristics without enough evidence that all rectangles describe the same visual element in the same coordinate space. The working magnifier is an important clue: it uses local HWND geometry directly, while persistent highlight depends on cross-process managed bounds and post-hoc conversion/fallback logic.

The `highlight_login_button.png` failure is especially useful because the overlay is roughly button-sized but far away from the visible **Log in** button. That points strongly at coordinate-space conversion or stale/wrong resolved bounds, not just an oversized ancestor fallback. The earlier `keep_highlighted_invalid_location.png` failure still points at an oversized/ancestor or DPI-scaled rectangle path.

### Lifecycle addendum from user retest
The user found a stronger reproduction pattern after the diagnostic build:

1. Start eM Client.
2. Open Settings.
3. `Keep Highlighted` works at first.
4. Close Settings.
5. Open Settings again.
6. `Keep Highlighted` positioning becomes wrong.

This shifts the most likely cause toward stale lifetime state. `EventTargetWindow` currently removes destroyed handles from `Desktop.ProxyCache`, but `MainForm` keeps `ControlProxy` instances in tree node tags and in `persistentHighlightProxy`. If the first Settings dialog is destroyed while persistent highlighting or stale tree nodes remain, the timer can continue using an old handle and old `managedChildPath`. On the next Settings dialog instance, Windows may reuse HWND values or the target control tree may have similar names but different live objects, causing persistent highlight to resolve geometry for the wrong live object. The magnifier still works because it starts from the current cursor position and current live HWNDs rather than cached tree proxies.

### Revised fix suggestion
The next behavioral fix should treat target window destruction as an invalidation event for persistent highlight:

1. Add a notification path from `EventTargetWindow`/`Desktop` to `MainForm` when `WindowDestroyed` and `HandleChanged` messages arrive.
2. When the destroyed handle matches `persistentHighlightProxy.Handle`, immediately stop persistent highlighting and clear the stale rectangle/overlay.
3. When the destroyed handle is found in the current tree, mark that branch stale or remove it so the user cannot keep operating on old Settings controls after the dialog closes.
4. On `HandleChanged`, update the cached proxy only if it is the same live control recreation; otherwise prefer disabling persistent highlight over tracking a potentially reused handle.
5. On the next **Keep Highlighted** action after reopening Settings, require a fresh tree/proxy path, either by user refresh or by auto-refreshing the relevant process/window branch.

This should be implemented before deeper DPI fixes because it directly matches the "first open works, second open breaks" lifecycle pattern and removes stale proxy/path reuse from the equation.

### Recommended next step
The diagnostic attempt has been implemented. The next implementation should address stale proxy/handle lifetime, then use the diagnostic log only if the lifecycle fix does not resolve the second-open failure. Ask the user to confirm whether to implement persistent-highlight invalidation on target window destruction.

1. `button_EmSync_Login` from `screenshots\highlight_login_button.png`
2. `optionButton_CustomSetup...` from `screenshots\keep_highlighted_invalid_location.png`
3. a known-good magnifier selection if practical

### Candidate fix after diagnostics
If diagnostics show that local `GetWindowRect(proxy.Handle)` is correct for controls with their own HWND, persistent highlight should use that as the primary source for those controls and only use target-process managed bounds for windowless child controls. If diagnostics show that target-process bounds are correct but only in physical coordinates, ManagedSpy should either become explicitly DPI-aware or use a deterministic conversion based on target/root DPI values rather than `PhysicalToLogicalPointForPerMonitorDPI(this.Handle)` alone.

### Validation plan
- Keep all existing screen-bounds tests.
- Add tests for any extracted decision helpers, especially:
  - "raw fallback rejected when raw rectangle is likely an ancestor"
  - "raw fallback accepted when raw rectangle and candidate are same visual element in different DPI spaces"
  - "conversion decision logs before/after rectangles and chosen source"
- Build with `.\build.ps1`.
- Run x64 and x86 `dotnet vstest` against built artifacts.
- Require user confirmation in eM Client before marking this bug fixed.
