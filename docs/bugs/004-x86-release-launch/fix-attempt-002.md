# Fix Attempt 002

## Attempt Status
- fixed

## Goal
- Restore a working startup dependency graph so `ManagedSpy` can resolve `ManagedSpyLib` and open normally under the matching-architecture host.

## Relation To Previous Attempts
- Follows `fix-attempt-001.md`.
- Attempt 001 improved launch guidance, but user retesting and local probing showed a deeper startup failure: both the x86 apphost and the x86 `dotnet` host exit immediately, and the matching-architecture host reports `FileNotFoundException` for `ManagedSpyLib`.

## Proposed Change
- Fix the project wiring in `ManagedSpy\ManagedSpy.csproj` so the native hook project is built and deployed without replacing the managed `ManagedSpyLib` runtime asset in `ManagedSpy.deps.json`.
- Add regression coverage that inspects the generated `ManagedSpy.deps.json` and the expected runtime files in the test output.

## Risks
- The hook shim still needs to deploy beside the app, so the fix must preserve native output placement while removing the broken managed dependency collision.
- Changes to project references can affect both direct project builds and the full solution build.

## Files And Components
- `ManagedSpy\ManagedSpy.csproj`
- `ManagedSpy.Tests\`
- `artifacts\release\<platform>\ManagedSpy.deps.json`

## Verification Plan
- Rebuild the repo for x86 and x64.
- Confirm `ManagedSpy.deps.json` lists `ManagedSpyLib.dll` as the managed runtime asset instead of `ManagedSpyHook.dll`.
- Run the existing test flow for x86 and x64.
- Launch the built app through the matching-architecture hosts and confirm it stays alive.

## Implementation Summary
- Updated `ManagedSpy\ManagedSpy.csproj` so the native hook project is still built under full MSBuild, but its output no longer participates as a managed assembly reference in the app dependency graph.
- Added an explicit post-build copy target for the default `ManagedSpy\bin\<platform>\<configuration>\` path so direct app builds still receive `ManagedSpyHook.dll`, its runtime metadata files, and `Ijwhost.dll`.
- Added `ManagedSpy.Tests\BuildOutputDependencyGraphTests.cs` to assert that `ManagedSpy.deps.json` exposes `ManagedSpyLib.dll` as the managed runtime asset and that the hook shim is still deployed beside the app.

## Test Results
- `ManagedSpy\ManagedSpy.csproj` built directly with full MSBuild for both `x64` and `x86`, and the direct app outputs now contain `ManagedSpyHook.dll`, `ManagedSpyHook.deps.json`, and `Ijwhost.dll`.
- `.\build.ps1` succeeded for both `x86` and `x64`.
- `dotnet vstest .\artifacts\release\x64\ManagedSpy.Tests.dll /Platform:x64` passed with 11/11 tests.
- `dotnet vstest .\artifacts\release\x86\ManagedSpy.Tests.dll /Platform:x86` passed with 11/11 tests.
- `artifacts\release\x86\ManagedSpy.deps.json` and `artifacts\release\x64\ManagedSpy.deps.json` now list `ManagedSpyLib.dll` as the runtime asset for `ManagedSpyLib/1.0.0`.
- Probed the x86 release after the fix:
  - `.\ManagedSpy.exe` stayed running for 5 seconds, reported `MainWindowTitle=Managed Spy`, and remained responsive.
  - `C:\Program Files (x86)\dotnet\dotnet.exe .\ManagedSpy.dll` stayed running for 5 seconds, reported `MainWindowTitle=Managed Spy`, and remained responsive.

## Outcome
- Local verification indicates the broken dependency graph is repaired and the x86 startup path now works through both the apphost and the matching-architecture `dotnet` host.
- User confirmed the fix after rebuilding and retesting the x86 artifact.

## Next Step
- None.

## Remaining Gaps
- None for this bug.
