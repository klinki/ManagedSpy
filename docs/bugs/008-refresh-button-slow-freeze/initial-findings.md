# Initial Findings

## Confirmed Facts
- `MainForm.RefreshWindows` currently runs all discovery and tree rebuilding synchronously on the UI thread.
- Refresh calls `ControlProxy.TopLevelWindows`, then filters with `ShowNative.Checked || cproxy.IsManaged`.
- `ControlProxy.IsManaged` checks `ComponentType` before checking whether `typeName` is already known.
- `ComponentType` now loads target assemblies with `Assembly.Load(File.ReadAllBytes(path))` to avoid locking target files.
- That no-lock loading is correct for the file-lock bug, but it is expensive and should not be triggered just to decide whether a proxy is managed during refresh.

## Likely Cause
- UI freeze: refresh does window discovery, managed-process probing, marshaled proxy creation, process metadata reads, and tree rebuilding on the UI thread.
- Slowdown regression: the refresh filter can cause byte-loaded target assembly/type resolution through `ComponentType`.

## Unknowns
- Exact distribution of refresh time in the user's environment.
- Whether the user's slowest case comes mostly from type resolution, process module scanning, target IPC, or process metadata reads.

## Reproduction Status
- Direct reproduction with the user's process set is not available in this environment.
- Code inspection confirms synchronous UI-thread refresh and a type-resolution fast-path bug.

## Evidence Gathered
- `ManagedSpy\MainForm.cs`
- `ManagedSpyLib\ControlProxy.cs`
- `ManagedSpyLib\Desktop.cs`

## 2026-05-29 Update
- Implementation confirmed the refresh path did not need `ComponentType`; checking known managed metadata first avoids byte-loading target assemblies during routine refresh filtering.
- Background refresh required synchronizing the shared proxy cache and process classification lists because discovery can now run outside the UI thread.
