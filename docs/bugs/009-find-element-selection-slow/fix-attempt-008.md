# Fix Attempt 008

## Attempt Status
awaiting-user-confirmation

## Goal
Keep **Find element on screen** responsive after attempt 007 restored the tree, because user retesting confirmed the tree is fixed but finder selection still freezes the UI.

## Relation To Previous Attempts
Attempt 007 fixed the missing top-level form regression and improved finder correctness by restoring managed-process tree roots. User confirmation after attempt 007 says the tree is fixed and finder is much better, but finder selection still freezes the ManagedSpy UI.

## Proposed Change
- Build the finder selection snapshot on a background thread:
  - target proxy
  - managed ancestor status
  - owning process metadata
  - top-level windows for the process
  - proxy parent chain
  - immediate children for each chain node
- Apply only the prepared snapshot to the TreeView on the UI thread.
- Stop eagerly fetching Layout information on every selection when the Layout tab is not visible.
- Replace the blocking flash animation's `Thread.Sleep` calls with an async delay-based implementation.

## Risks
- The target control tree can change between background snapshot creation and UI application; the existing handle-based matching and lazy refresh behavior should recover on the next expansion/refresh.
- Layout information is now loaded when the Layout tab is selected rather than for every tree selection.

## Expected Verification
- Build with `.\build.ps1`.
- Run x64 artifact tests.
- Run x86 artifact tests.
- Copy updated binaries into `artifacts\release` for manual retesting.

## Files Or Components Involved
- `ManagedSpy\MainForm.cs`
- `docs\bugs\009-find-element-selection-slow\*`

## Actual Implementation Summary
- Added a `FinderSelectionSnapshot` built on a background task so the target proxy lookup, managed ancestor checks, top-level process windows, parent chain, and immediate child lists are prepared off the UI thread.
- Updated finder selection to apply only the prepared snapshot to the `TreeView` on the UI thread.
- Deferred Layout data retrieval until the Layout tab is visible, avoiding a synchronous layout query during finder selection on the Properties tab.
- Changed the finder flash animation from UI-thread `Thread.Sleep` calls to an async `Task.Delay` sequence.

## Test And Verification Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt34-build` passed.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt34-build\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 17/17.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt34-build\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 17/17.
- Copied validated `ManagedSpy`, `ManagedSpyLib`, and hook binaries into `artifacts\release\x86` and `artifacts\release\x64`.

## Outcome And Remaining Gaps
- Awaiting user confirmation that **Find element on screen** no longer freezes the UI in their target application.
