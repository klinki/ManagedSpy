ManagedSpy
==========

ManagedSpy is the software for runtime introspect .NET Windows Forms programs.
The current codebase targets **.NET 10** on Windows.

Most of the original C++/CLI implementation has been ported to managed C#.
The remaining native piece is a thin `ManagedSpyHook` shim that exports the
`SetWindowsHookEx` callback used for cross-process inspection. Windows requires
that injected hook entry point to come from a DLL export, so this boundary
cannot be replaced by a pure IL assembly without changing the architecture.

Platform support
----------------

Both 32-bit and 64-bit processes are supported. There are two distinct build
configurations for 32 and 64-bit support. 32-bit ManangedSpy can only inspect
32-bit processes, and 64-bit ManagedSpy can only inspect 64-bit processes.

Build requirements
------------------

- .NET 10 SDK
- Visual Studio 2026+ (or Build Tools) with:
  - MSBuild
  - Desktop development with C++
  - Windows 10 SDK `10.0.26100.0`

Build
-----

```powershell
.\build.ps1
```

By default this builds Release for both `x86` and `x64` into `artifacts\release\`.

Useful options:

```powershell
.\build.ps1 -Platform x64
.\build.ps1 -OutputRoot .\artifacts\my-release
.\build.ps1 -MSBuildPath "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe"
```

Run
---

Launch the generated executable from the matching platform folder:

```powershell
.\artifacts\release\x64\ManagedSpy.exe
.\artifacts\release\x86\ManagedSpy.exe
```

If you want to start the DLL directly, the `dotnet` host must match the build
architecture. The x86 build will fail if you run `dotnet .\ManagedSpy.dll`
through the usual x64 `dotnet` on PATH.

```powershell
# x64
dotnet .\artifacts\release\x64\ManagedSpy.dll

# x86
& "${env:ProgramFiles(x86)}\dotnet\dotnet.exe" .\artifacts\release\x86\ManagedSpy.dll
```

Each output folder also includes `README-launch.txt` with the platform-specific
launch command.

Test
----

For full validation, build the release artifacts and run the tests from the
artifact folder for each platform:

```powershell
.\build.ps1
dotnet vstest .\artifacts\release\x64\ManagedSpy.Tests.dll /Platform:x64
dotnet vstest .\artifacts\release\x86\ManagedSpy.Tests.dll /Platform:x86
```

This path validates the generated dependency graph, native hook deployment, and
the UIAutomation workflow that launches `ManagedSpy.exe` against the bundled
test target app. The UIAutomation test requires an interactive Windows desktop.

SDK-only test runs are useful for quick unit feedback, but artifact-dependent
tests are reported as inconclusive when the native hook and executable outputs
are not present:

```powershell
dotnet test .\ManagedSpy.Tests\ManagedSpy.Tests.csproj -p:Platform=x64
```

Download
--------

To download ManagedSpy binary distributions, please visit [GitHub releases
section][releases].

Prepare release
---------------

There's a [Prepare-Release][prepare-release] script to build and pack the new
release.

License
-------

Original source code obtained from http://msdn.microsoft.com/en-us/magazine/cc163617.aspx

Please, see [EULA][eula] for additional license information.

[eula]: EULA.doc
[prepare-release]: scripts/Prepare-Release.ps1

[releases]: https://github.com/ForNeVeR/ManagedSpy/releases
