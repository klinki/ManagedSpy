# Fix Attempt 001

## Attempt Status
- insufficient

## Goal
- Make the x86 release launch path reliable and obvious so users do not hit an architecture mismatch when starting the 32-bit build.

## Relation To Previous Attempts
- First attempt for this bug.

## Proposed Change
- Verify which x86 launch entry points already work.
- If `ManagedSpy.exe` works, make the supported x86 launch path explicit in the repo and release flow.
- If the current x86 apphost does not work, adjust the build or release packaging so the x86 output includes a working launcher.

## Risks
- Changing platform targeting would be risky because the product intentionally separates x86 and x64 inspection.
- A documentation-only fix would be insufficient if the packaged x86 launcher is itself broken.

## Files And Components
- `ManagedSpy\ManagedSpy.csproj`
- `build.ps1`
- `Readme.md`
- release artifacts under `artifacts\release\x86`

## Verification Plan
- Reproduce the reported failure with `dotnet .\ManagedSpy.dll` from the x86 output.
- Probe `ManagedSpy.exe` and the x86 `dotnet` host path.
- Build the repo and run the supported test flow after any changes.

## Implementation Summary
- Updated `build.ps1` to write `README-launch.txt` into each platform output folder after the build completes.
- Added platform-specific launch guidance to `Readme.md`, including the explicit x86 `dotnet` path when someone needs to run the DLL directly.
- Kept the app architecture split intact; the fix only clarifies and ships the supported launch path instead of weakening platform targeting.

## Test Results
- Reproduced the reported failure with `C:\Program Files\dotnet\dotnet.exe .\ManagedSpy.dll` from `artifacts\release\x86`, which throws the expected architecture-mismatch `FileLoadException`.
- `.\build.ps1` succeeded and now writes `README-launch.txt` to both `artifacts\release\x86\` and `artifacts\release\x64\`.
- `dotnet vstest .\artifacts\release\x64\ManagedSpy.Tests.dll /Platform:x64` passed with 10/10 tests.
- `dotnet vstest .\artifacts\release\x86\ManagedSpy.Tests.dll /Platform:x86` passed with 10/10 tests.
- User retesting showed `.\ManagedSpy.exe` still does not launch visibly, and the original x64-host DLL command still fails with the architecture mismatch.

## Outcome
- The build and documentation now make the supported launch path explicit: use `ManagedSpy.exe`, or use the matching-architecture `dotnet` host when running the DLL directly.
- The documentation improvement is still useful, but it was not sufficient. The app also has a real startup failure when launched through the matching-architecture host.

## Next Step
- Continue with a second attempt focused on the startup binding failure for `ManagedSpyLib`.

## Remaining Gaps
- The application still exits at startup on both x86 and x64 when run through the matching-architecture host.
