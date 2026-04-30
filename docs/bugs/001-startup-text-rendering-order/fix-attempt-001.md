# Fix Attempt 001

## Attempt Status
- fixed

## Goal
- Restore a safe WinForms startup order so `SetCompatibleTextRenderingDefault(false)` runs before any `IWin32Window` is created.

## Relation To Previous Attempts
- First attempt for this bug in the worktree.

## Proposed Change
- Add a small startup-initialization helper that centralizes the required call order.
- Update `Program.Main` to use that helper.
- Add a regression test that proves WinForms setup happens before message-filter initialization.

## Risks
- Minimal functional risk; the change is limited to startup sequencing and a focused test.

## Files And Components
- `ManagedSpy\Program.cs`
- `ManagedSpy\StartupInitialization.cs`
- `ManagedSpy.Tests\ManagedSpy.Tests.csproj`
- `ManagedSpy.Tests\StartupInitializationTests.cs`

## Verification Plan
- Build the worktree.
- Run the existing MSTest suite.
- Confirm the regression test covers the ordering contract directly.

## Implementation Summary
- Added `ManagedSpy\StartupInitialization.cs` to centralize startup ordering.
- Updated `ManagedSpy\Program.cs` to call the helper instead of initializing message filters directly.
- Added `ManagedSpy.Tests\StartupInitializationTests.cs` and referenced the app project from the test project so the ordering contract is covered.
- Exposed `StartupInitialization.Initialize` publicly so the regression test can call it directly without duplicating the startup logic.

## Test Results
- `.\build.ps1 -Platform x64` succeeded.
- `dotnet vstest .\artifacts\release\x64\ManagedSpy.Tests.dll /Platform:x64` passed with 10/10 tests.

## Outcome
- Local verification indicates the startup-order regression is repaired and covered by an automated test.
- User confirmed the repaired worktree starts correctly.

## Next Step
- None.

## Remaining Gaps
- None for this bug.
