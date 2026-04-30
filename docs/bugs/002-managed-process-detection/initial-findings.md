# Initial Findings

## Confirmed Facts
- `Desktop.EnableHook` calls `EnsureHookModuleLoaded()`, which loads `ManagedSpyHook.dll` from `AppContext.BaseDirectory`.
- A normal `dotnet build .\ManagedSpy\ManagedSpy.csproj -c Release -p:Platform=x64` places `ManagedSpy.dll` and `ManagedSpyLib.dll` in `ManagedSpy\bin\x64\Release\`, but does **not** place `ManagedSpyHook.dll` there.
- The app project currently references only `ManagedSpyLib.csproj`, not the hook shim project.
- If `ManagedSpyHook.dll` is missing, `EnableHook` returns early and the managed-message path never activates.

## Likely Cause
- The port removed the old direct dependency on the C++/CLI project, but did not replace it with a build/deployment dependency for the remaining hook shim. As a result, the normal app output is missing the DLL required for `SetWindowsHookEx`.

## Unknowns
- None required for the repair. The missing hook shim in the normal app output fully explains the reported behavior.

## Reproduction Status
- Reproduced by building the app project directly and comparing the resulting output contents against the hook-loading path in code.

## Evidence Gathered
- `ManagedSpy\ManagedSpy.csproj` references only `..\ManagedSpyLib\ManagedSpyLib.csproj`.
- `ManagedSpyLib\Desktop.cs` loads `ManagedSpyHook.dll` from `AppContext.BaseDirectory`.
- Direct app build output contains `ManagedSpy.dll` and `ManagedSpyLib.dll`, but not `ManagedSpyHook.dll`.
