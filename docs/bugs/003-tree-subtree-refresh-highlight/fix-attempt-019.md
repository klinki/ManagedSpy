# Fix Attempt 019

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Use a non-accessibility geometry path immediately on highlight target switches when that path can replace the previous rectangle right away.

## Relation To Previous Attempts
Follow-up to the diagnostic `fix-attempt-018.md`. The user log showed a bad switch where:
- the preferred rectangle source was chosen,
- the preferred rectangle later drifted to a different location only after clicking the target app,
- the log did not indicate any non-accessibility retry on that transition.

That makes the switch-time accessibility-first choice the most likely cause of the remaining lag.

## Proposed Change
- On target switches, query both:
  - the preferred rectangle
  - the non-accessibility rectangle
- If the non-accessibility rectangle is valid and different from the previously highlighted rectangle, use it immediately for that switch.

## Risks
- The non-accessibility path may be less accurate for some custom controls, so this preference is limited to target switches.
- The preferred accessibility path still remains available for initial selection and later steady-state updates.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the existing helper coverage in x64.
- User retest on repeated highlight switching between different nodes.

## Implementation Summary
- Changed the switch-time highlight selection logic in `MainForm`.
- On target switches, `TryGetPersistentHighlightRectangle` now queries the non-accessibility path up front and prefers it when it can immediately replace the old highlighted rectangle.
- Kept the preferred accessibility path as the default when there is no prior highlight rectangle yet.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt19` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt19\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 4/4 tests.

## Outcome
Persistent highlight switches no longer have to wait for the preferred accessibility rectangle to catch up before trying the non-accessibility geometry source.

## Next Step
User confirmation on whether switching between highlighted nodes now updates immediately.

## Remaining Gaps
- Behavioral confirmation in the user's application set is still pending.
