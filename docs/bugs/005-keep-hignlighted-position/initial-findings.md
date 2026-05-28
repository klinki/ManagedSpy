# Initial Findings

## Confirmed Facts
- The user reported attempt 028 did not fix `Keep Highlighted` positioning.
- `screenshots\highlight_login_button.png` shows `button_EmSync_Login` selected while the red overlay is at the lower-right area of the eM Client Settings window instead of around the visible **Log in** button.
- The screenshot's property grid shows the selected button size as `74; 41`, while image measurement found the red overlay bounds at approximately `1271,976,1388,1043` in the screenshot image, size `118x68`.
- `screenshots\keep_highlighted_invalid_location.png` shows a much larger misplaced overlay; image measurement found red bounds at approximately `310,538,2165,1163`, size `1856x626`.
- The user reports the **Find elements on the screen** magnifier icon works correctly.
- The magnifier path in `ManagedSpy\MainForm.cs` uses direct local `GetWindowRect` via `TryGetWindowRectangle`.
- The persistent path in `ManagedSpy\MainForm.cs` calls `proxy.GetScreenBounds()` and only later tries raw-window fallbacks.
- ManagedSpy currently has no explicit DPI-awareness setup in `Program.cs` or `app.config`.
- The user found a reproducible lifecycle clue: `Keep Highlighted` works after starting eM Client and opening Settings the first time, then becomes wrong after closing Settings and opening it again.

## Likely Cause
- Persistent highlight mixes rectangles from two coordinate sources:
  - target-process managed/accessibility bounds returned by `proxy.GetScreenBounds()`
  - local `GetWindowRect` rectangles observed by the ManagedSpy process
- In a DPI-aware target app such as eM Client, these sources can be in different DPI/virtualization spaces.
- The existing fallback logic may also compare a selected child-control rectangle against a raw HWND rectangle that belongs to an ancestor/container rather than the same visual element.
- The Settings close/reopen clue points to stale `ControlProxy` state: `persistentHighlightProxy` and tree node tags can keep old handles/managed paths after a target dialog is destroyed, while the current code only removes destroyed handles from `Desktop.ProxyCache` and does not notify `MainForm` to disable persistent highlight or invalidate tree nodes.

## Unknowns
- Whether the bad `button_EmSync_Login` rectangle came from the preferred/accessibility path, non-accessibility path, raw fallback, or local normalization.
- Whether the selected button has its own HWND in eM Client.
- Whether the target-process rectangle is correct before `NormalizeRectangleToLocalCoordinates` runs.
- Whether the local raw rectangle is correct for this selected tree node or only correct for magnifier cursor hits.
- Whether the wrong second-open highlight is caused by handle reuse, stale `managedChildPath`, or both.

## Reproduction Status
- Direct local reproduction in eM Client is not available in this environment.
- The bug is reproduced indirectly from user screenshots and code inspection.

## Evidence Gathered
- `screenshots\highlight_login_button.png`
- `screenshots\keep_highlighted_invalid_location.png`
- `ManagedSpy\MainForm.cs`
- `ManagedSpyLib\ControlProxy.cs`
- `ManagedSpyLib\Desktop.cs`
- `ManagedSpyLib\ScreenBoundsHelper.cs`
- Opus 4.6 parallel research pass, recorded in `research.md`
