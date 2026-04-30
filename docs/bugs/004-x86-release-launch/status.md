# Bug Status

## Current State
- fixed

## Active Attempt
- `fix-attempt-002.md`

## Last Updated
- 2026-04-30

## Confirmation Date
- 2026-04-30

## Resolution Summary
- `build.ps1` now writes `README-launch.txt` into each platform output folder so the artifacts themselves explain how to launch the build.
- `Readme.md` now documents that the x86 DLL cannot be started with the usual x64 `dotnet` on PATH and shows the correct x86 host command.
- The fix keeps the intended x86/x64 split intact instead of changing platform targeting.
- Local retesting showed an additional startup bug: under the matching-architecture host, `ManagedSpy` fails to resolve `ManagedSpyLib` and exits immediately.
- `ManagedSpy\ManagedSpy.csproj` now keeps the native hook project out of the managed runtime dependency graph by setting `ReferenceOutputAssembly="false"` on the vcxproj reference.
- A post-build copy target restores `ManagedSpyHook.dll`, its runtime metadata, and `Ijwhost.dll` to the default direct-build app output, so direct project builds still run correctly.
- The MSTest suite now includes a regression that inspects `ManagedSpy.deps.json` to ensure `ManagedSpyLib.dll` remains the managed runtime asset while the hook shim still deploys.
- User confirmed that rebuilding and launching the x86 release now works in the original workflow.

## Attempt History
- `fix-attempt-001.md` - investigate the architecture mismatch and make the x86 launch path reliable.
- `fix-attempt-002.md` - repair the startup dependency graph so the managed library resolves correctly.

## State Change Log
- 2026-04-30: bug opened from user report that `dotnet .\ManagedSpy.dll` fails in `artifacts\release\x86` with an architecture mismatch.
- 2026-04-30: investigation started; current evidence points to launching the x86 app through the x64 `dotnet` host on PATH.
- 2026-04-30: fix attempt 001 implemented by adding platform-specific launch guidance to the build output and repository README.
- 2026-04-30: local build and x86/x64 test verification passed; awaiting user confirmation.
- 2026-04-30: user reported the bug is still present; `ManagedSpy.exe` still does not launch visibly and the original DLL command still fails.
- 2026-04-30: local repro with the matching-architecture hosts showed startup exits with `FileNotFoundException` for `ManagedSpyLib`.
- 2026-04-30: fix attempt 002 started to repair the project dependency graph.
- 2026-04-30: fix attempt 002 implemented by separating the native hook project from the managed dependency graph and copying hook artifacts explicitly for direct app builds.
- 2026-04-30: local build, test, deps-graph, and x86 startup verification passed; awaiting user confirmation.
- 2026-04-30: user confirmed the x86 release now starts correctly after rebuilding.

## Notes
- Keep the x86 and x64 build split intact unless investigation proves the current platform targeting is wrong.
- Closed after explicit user confirmation.
