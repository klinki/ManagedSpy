# Fix Attempt 007

## Attempt Status
awaiting-user-confirmation

## Goal
Fix the underlying tree regression where refresh shows only one top-level form for a managed application that has multiple top-level forms.

## Relation To Previous Attempts
Attempt 006 improved finder targeting and visibility, but user retesting confirmed the broader tree view is still degraded. The user reported that an application with two main forms now shows only one, while older builds showed all forms.

## Proposed Change
- Review changes since `47c0a5754d32a5cb2f1117c17b33a39986132d92` for tree/filtering regressions.
- Stop using each individual top-level HWND as the only inclusion gate when **Show Native Windows** is off.
- Treat compatible managed target processes as the managed boundary and include all their top-level windows in the process tree.
- Apply the same process-level inclusion rule to finder-assisted top-level population.
- Validate cached proxies against the current HWND owning process before reusing them.
- Add a regression target/test with two top-level WinForms forms.

## Risks
- A managed process can expose native top-level helper windows when **Show Native Windows** is off. This is preferable to hiding real managed forms and matches the user's expectation that all forms from the target application remain visible.

## Expected Verification
- Build with `.\build.ps1`.
- Run x64 artifact tests.
- Run x86 artifact tests.
- Copy updated binaries into `artifacts\release` for manual retesting.

## Files Or Components Involved
- `ManagedSpy\MainForm.cs`
- `ManagedSpyLib\ControlProxy.cs`
- `ManagedSpyLib\Desktop.cs`
- `ManagedSpy.TestTarget\Program.cs`
- `ManagedSpy.Tests\ManagedSpyEndToEndTests.cs`
- `docs\bugs\009-find-element-selection-slow\*`

## Actual Implementation Summary
- Added `ControlProxy.IsManagedProcess(int)` so refresh and finder tree population can make process-level inclusion decisions.
- `BuildRefreshSnapshot` now caches process managed-state and includes every top-level window from compatible managed target processes, instead of dropping a top-level HWND solely because that specific HWND did not answer as managed.
- `PopulateProcessTopLevelWindows` now uses the same process-level inclusion rule for finder-created process branches.
- `Desktop.GetProxy` now validates cached proxies against the HWND's current owning process and retries managed proxy hydration for cached native proxies in managed processes.
- Added a second top-level WinForms form to `ManagedSpy.TestTarget`.
- Extended the UIAutomation end-to-end test to expand the target process node and verify the secondary top-level form is present.

## Test And Verification Results
- Initial attempt31 x64 test failed because `Process.MainWindowHandle` can point at the secondary test form; updated the test helper to locate top-level windows by process/title.
- Initial attempt31/attempt32 test validation also showed cached native proxies could hide managed form names, which led to the managed-proxy upgrade fix in `Desktop.GetProxy`.
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt33-build` passed.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt33-build\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 17/17.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt33-build\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 17/17.
- Copied validated `ManagedSpy`, `ManagedSpyLib`, and hook binaries into `artifacts\release\x86` and `artifacts\release\x64`.

## Outcome And Remaining Gaps
- Awaiting user confirmation that the tree again shows all top-level forms for their target application and that **Find element on screen** works correctly against the restored tree.
