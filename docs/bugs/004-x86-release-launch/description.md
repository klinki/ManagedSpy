# Bug Description

## Title

Running the x86 release via `dotnet .\ManagedSpy.dll` fails with an architecture mismatch

## Status
- fixed

## Reported Symptoms

- From `artifacts\release\x86`, running `dotnet .\ManagedSpy.dll` throws `System.IO.FileLoadException`.
- The exception says the assembly architecture is not compatible with the current process architecture.
- Running `.\ManagedSpy.exe` appears to do nothing from PowerShell.
- Running the app through the matching-architecture host exits immediately with `System.IO.FileNotFoundException` for `ManagedSpyLib`.

## Expected Behavior

- The x86 release should have a clear, working launch path for 32-bit ManagedSpy.

## Actual Behavior

- Launching the x86 app through the `dotnet` host on PATH fails before the app starts.
- Launching through the x86 apphost or the x86 `dotnet` host also fails before the main window appears because `ManagedSpyLib` is not resolved at startup.

## Reproduction Details

1. Build the repo with `.\build.ps1`.
2. Change to `artifacts\release\x86`.
3. Run `dotnet .\ManagedSpy.dll`.
4. Observe the architecture-mismatch `FileLoadException`.

## Affected Area

- Release launch flow for `ManagedSpy` x86 artifacts.
- Build output expectations and release documentation.
- Project dependency wiring in `ManagedSpy\ManagedSpy.csproj`.

## Constraints

- The project intentionally ships separate `x86` and `x64` builds.
- `ManagedSpy` is a Windows Forms app with a native hook shim, so platform-specific binaries are expected.

## Open Questions

- None currently. The dependency-graph collision has been identified and repaired locally.
