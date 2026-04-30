# Fix Attempt 002

## Attempt Status
- fixed

## Goal
- Eliminate the freeze when another ManagedSpy instance starts and make the injected hook resolve the managed bridge reliably from the hook DLL location.

## Relation To Previous Attempts
- Follows `fix-attempt-001.md`.
- Attempt 001 restored `ManagedSpyHook.dll` deployment for the supported MSBuild/Visual Studio path, but user retesting showed the scenario is still broken and now freezes when another ManagedSpy instance starts.

## Proposed Change
- Stop resolving `ManagedSpyLib` by simple assembly name inside `ManagedSpyHook.dll`.
- Resolve and load `ManagedSpyLib.dll` from the same directory as `ManagedSpyHook.dll`, reusing an already loaded copy when available.
- Replace unbounded cross-process `SendMessage` calls in the managed transport layer with timeout-based sends so a stuck target cannot freeze the inspector indefinitely.

## Risks
- The hook path is sensitive: incorrect assembly loading or message transport changes could stop managed detection entirely.
- Timeout-based sends may convert a hang into a failed managed-proxy lookup, so they must preserve the existing fallback behavior.

## Files And Components
- `ManagedSpyLib\HookShim.cpp`
- `ManagedSpyLib\NativeMethods.cs`
- `ManagedSpyLib\MemoryStore.cs`

## Verification Plan
- Rebuild the app with MSBuild and confirm the hook shim still deploys beside the executable.
- Run the existing solution build/test flow.
- Retest with the user against the previously freezing two-instance scenario.

## Implementation Summary
- Updated `ManagedSpyLib\HookShim.cpp` to resolve `ManagedSpyLib.dll` from the hook DLL directory and reuse an already loaded `ManagedSpyLib` assembly when possible.
- Updated `ManagedSpyLib\MemoryStore.cs` to use timeout-based cross-process sends for both request and release messages.
- Added the required `SendMessageTimeout` interop in `ManagedSpyLib\NativeMethods.cs`.

## Test Results
- `MSBuild.exe ManagedSpy\ManagedSpy.csproj /t:Restore,Build /p:Configuration=Release /p:Platform=x64` succeeded and still places `ManagedSpyHook.dll` beside `ManagedSpy.exe`.
- `.\build.ps1 -Platform x64` succeeded.
- `dotnet vstest .\artifacts\release\x64\ManagedSpy.Tests.dll /Platform:x64` passed with 10/10 tests.

## Outcome
- Local verification passed, but the exact two-instance UI scenario still requires user confirmation.
- User confirmed that the freeze is gone and managed detection works in the real scenario.

## Next Step
- None.

## Remaining Gaps
- None for this bug.
