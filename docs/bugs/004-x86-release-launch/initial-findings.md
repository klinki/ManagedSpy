# Initial Findings

## Confirmed Facts

- `ManagedSpy\ManagedSpy.csproj` targets `net10.0-windows`, builds both `x86` and `x64`, and sets `PlatformTarget` explicitly per platform.
- The x86 release output already contains both `ManagedSpy.dll` and `ManagedSpy.exe`.
- `dotnet` on PATH resolves to `C:\Program Files\dotnet\dotnet.exe`, which is the x64 host on this machine.
- An x86 .NET installation is also present at `C:\Program Files (x86)\dotnet\dotnet.exe`, including `Microsoft.NETCore.App 10.0.5` and `Microsoft.WindowsDesktop.App 10.0.5`.

## Likely Cause

The reported failure is consistent with launching a platform-specific x86 framework-dependent app through the x64 `dotnet` host. The assembly itself is marked x86, so `dotnet .\ManagedSpy.dll` uses the wrong process architecture when `dotnet` resolves to the x64 installation.

## Unknowns

- Whether `ManagedSpy.exe` already launches successfully from the x86 release output.
- Whether release users are expected to run the generated apphost executable or the DLL directly.
- Whether a build/publish change is needed, or whether the missing piece is just launch guidance.

## Reproduction Status

- Reproduced from the user report and corroborated by the current build and runtime layout.

## Evidence Gathered

- `ManagedSpy\ManagedSpy.csproj`
- `ManagedSpyLib\ManagedSpyLib.csproj`
- `Readme.md`
- `artifacts\release\x86\`
- Local runtime inventory showing separate x64 and x86 `dotnet` installations
