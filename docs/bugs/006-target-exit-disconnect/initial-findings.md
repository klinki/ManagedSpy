# Initial Findings

## Confirmed Facts
- `ControlProxy.ComponentType` loads every path captured from the target AppDomain with `Assembly.LoadFile`.
- Assemblies loaded into ManagedSpy's default process cannot be unloaded individually.
- Loading assemblies directly from target file paths can keep those files locked after the target application exits.
- `RefreshWindows` stores `Process` objects as process tree node tags but does not subscribe to process exit or explicitly dispose old process objects when rebuilding the tree.
- Target window destruction currently removes individual proxy handles, but there is no process-level disconnect path.

## Likely Cause
- The file-lock symptom is most likely caused by `Assembly.LoadFile(assemblyPath)` in `ControlProxy.ComponentType`.
- The "does not disconnect" symptom is likely caused by missing process-exit handling in `MainForm`, leaving stale process/proxy/tree state visible after the target exits.

## Unknowns
- Whether any target assembly metadata scenario depends on `Assembly.Location` after loading.
- Whether target apps with many dynamic assemblies need additional filtering before metadata loading.

## Reproduction Status
- Direct reproduction with the user's target is not available in this environment.
- Code inspection confirms a plausible permanent file-lock path.

## Evidence Gathered
- `ManagedSpyLib\ControlProxy.cs`
- `ManagedSpyLib\Desktop.cs`
- `ManagedSpy\MainForm.cs`

## Update 2026-05-28
- A focused review found that process-exit cache cleanup should not rely on live HWND ownership after the target has exited, because destroyed HWNDs may no longer report their original process.
- `ControlProxy` needs stable captured process ownership so `Desktop` can remove stale cached proxies even after target windows are gone.
