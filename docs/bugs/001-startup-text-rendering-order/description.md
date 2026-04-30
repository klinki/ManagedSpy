# Bug Description

## Title
Startup initialization creates a WinForms window before `SetCompatibleTextRenderingDefault`

## Status
- fixed

## Reported Symptoms
- Application startup throws `System.InvalidOperationException`.
- The exception says `SetCompatibleTextRenderingDefault` must be called before the first `IWin32Window` object is created.
- The failure occurs in `ManagedSpy.Program.Main` when the worktree build is launched.

## Expected Behavior
- ManagedSpy should finish WinForms startup initialization without throwing.
- `Application.SetCompatibleTextRenderingDefault(false)` should run before any code creates a WinForms window or control handle.

## Actual Behavior
- `MessageFilters.Initialize()` runs first.
- That code touches `Desktop.EventWindow.Handle`, which creates a WinForms control too early.
- `Application.SetCompatibleTextRenderingDefault(false)` then throws.

## Reproduction Details
1. Build and run the ported worktree at `E:\projects\ManagedSpy-net10-port`.
2. Startup enters `ManagedSpy.Program.Main`.
3. `MessageFilters.Initialize()` creates the first WinForms window.
4. `Application.SetCompatibleTextRenderingDefault(false)` throws immediately afterward.

## Affected Area
- `ManagedSpy\Program.cs`
- WinForms startup ordering around `MessageFilters.Initialize()`

## Constraints
- Preserve the current managed-port structure.
- Keep `MessageFilters.Initialize()` available, but only after the required WinForms startup calls.
- Add regression coverage using the existing MSTest test project.

## Open Questions
- None at the moment.
