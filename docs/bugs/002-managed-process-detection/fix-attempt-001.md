# Fix Attempt 001

## Attempt Status
- insufficient

## Goal
- Restore managed application detection by ensuring the hook shim is built and deployed beside the normal app output.

## Relation To Previous Attempts
- First attempt for this bug.

## Proposed Change
- Add the hook shim project back as an app dependency so a normal app build produces `ManagedSpyHook.dll` beside `ManagedSpy.exe`.
- Keep the managed library reference in place.
- Rebuild the normal app project and confirm the hook shim is present.

## Risks
- Low. The change is limited to build wiring and should not alter runtime logic beyond making the existing hook path available again.

## Files And Components
- `ManagedSpy\ManagedSpy.csproj`
- `ManagedSpy.Tests\ManagedSpy.Tests.csproj` (only if extra deployment coverage is needed)

## Verification Plan
- Build `ManagedSpy\ManagedSpy.csproj` directly and confirm `ManagedSpyHook.dll` is present beside the app.
- Run the existing solution build/test flow to ensure the project graph still works.

## Implementation Summary
- Restored the hook shim as an app dependency for MSBuild/Visual Studio builds by adding a conditional project reference in `ManagedSpy\ManagedSpy.csproj`.
- Kept the managed library reference unchanged.
- Avoided breaking `dotnet build` by only including the C++/CLI project when the build is running on full MSBuild rather than the .NET CLI host.

## Test Results
- `MSBuild.exe ManagedSpy\ManagedSpy.csproj /t:Restore,Build /p:Configuration=Release /p:Platform=x64` now places `ManagedSpyHook.dll` beside `ManagedSpy.exe` in `ManagedSpy\bin\x64\Release\`.
- `.\build.ps1 -Platform x64` succeeded.
- `dotnet vstest .\artifacts\release\x64\ManagedSpy.Tests.dll /Platform:x64` passed with 10/10 tests.

## Outcome
- Local verification indicates the missing hook shim deployment was restored for the supported build path, which should restore managed app detection.
- User retesting showed the bug remained unresolved and the two-instance scenario could freeze.

## Next Step
- Continue with a second attempt focused on hook bridge loading and transport robustness.

## Remaining Gaps
- The original scenario still fails and can freeze the app.
