# Initial Findings

## Confirmed Facts
- `Program.Main` currently calls `MessageFilters.Initialize()` before `Application.EnableVisualStyles()` and `Application.SetCompatibleTextRenderingDefault(false)`.
- `MessageFilters.Initialize()` accesses `Desktop.EventWindow.Handle`.
- `Desktop.EventWindow` is a WinForms `Control`, so accessing its handle creates the first `IWin32Window`.
- The reported exception matches that call order exactly.

## Likely Cause
- The managed port kept the old direct startup sequence in `Program.cs` instead of preserving the safer initialization wrapper that calls WinForms setup first and message-filter initialization last.

## Unknowns
- None needed for the code fix. The failing order is already visible from source inspection.

## Reproduction Status
- Reproduced by code inspection against the reported stack trace.

## Evidence Gathered
- `ManagedSpy\Program.cs` shows:
  - `MessageFilters.Initialize();`
  - `Application.EnableVisualStyles();`
  - `Application.SetCompatibleTextRenderingDefault(false);`
- The exception points at `Program.cs:line 15`, which is the late `SetCompatibleTextRenderingDefault(false)` call.
