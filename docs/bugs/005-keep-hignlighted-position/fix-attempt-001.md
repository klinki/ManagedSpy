# Fix Attempt 001

## Attempt Status
implemented-awaiting-user-diagnostics

## Goal
Add diagnostic instrumentation for persistent `Keep Highlighted` geometry decisions so the next eM Client retest can identify whether the bad overlay comes from target-process bounds, local DPI normalization, raw HWND fallback, or stale/wrong control path resolution.

## Relation To Previous Attempts
This follows the research-only intake for `005-keep-hignlighted-position` and the failed `003-tree-subtree-refresh-highlight` attempt 028. The user reported attempt 028 was still broken and specifically requested debug information plus research before proceeding.

## Proposed Change
- Extend `ManagedSpy-highlight-diagnostics.log` lines with pre-normalization and post-normalization rectangles.
- Log proxy/root/parent handles and rectangles, process ids, DPI values, managed child path details, raw/root equivalence, fallback flags, and fallback comparison metrics.
- Expose non-browsable `ControlProxy` managed-child-path diagnostics so `MainForm` can log whether the selected tree node represents a nested managed child.
- Do not change the highlight selection behavior in this attempt; only add diagnostic data.

## Risks
- Diagnostics may produce wider log lines, but logging is already limited to target changes or rectangle changes.
- `GetDpiForWindow` may not be available on older Windows versions, so diagnostic logging must report `unavailable` rather than failing.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `ManagedSpyLib\ControlProxy.cs`
- `docs\bugs\005-keep-hignlighted-position\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64` into an isolated output root.
- Run `ManagedSpy.Tests.dll` from both built outputs with matching `/Platform` values.
- Ask the user to retest the two eM Client scenarios and share `ManagedSpy-highlight-diagnostics.log`.

## Implementation Summary
- Extended `ManagedSpy-highlight-diagnostics.log` with `diagnosticVersion=2` entries.
- Added handle, process, DPI, managed child path, parent/root rectangle, raw/root equality, pre-normalization rectangle, post-normalization rectangle, normalization-change, raw-fallback, and fallback-metric fields.
- Exposed non-browsable `ControlProxy.ManagedChildPathLength` and `ControlProxy.ManagedChildPath` for diagnostics without changing visible property-grid behavior.
- Left highlight selection behavior unchanged so this attempt only gathers evidence for the next fix.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\diagnostics-attempt1-build` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\diagnostics-attempt1-build\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 14/14 tests.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\diagnostics-attempt1-build\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 14/14 tests.

## Outcome
- Diagnostic-only build is ready for eM Client retesting.
- The next log should identify whether the bad `Keep Highlighted` rectangle is chosen from target-process bounds, local normalization, raw HWND fallback, or stale path resolution.

## Follow-up Update
- 2026-05-27: user reported the highlight now appears at the right position in the diagnostic build.
- This attempt did not intentionally change highlight selection behavior; it added logging and exposed non-browsable path diagnostics only.
- Possible explanations include timing/state differences from the extra diagnostics, using a freshly built artifact that includes the prior attempt 028 changes, target window/control state changes, DPI/window placement changes, or stale overlay state being reset by restarting.

## Next Step
- Ask the user to repeat the retest after restarting ManagedSpy/eM Client and share the diagnostic log if possible.

## Remaining Gaps
- Direct reproduction in eM Client is still unavailable in this environment.
- No behavioral fix is included in this attempt; it only adds diagnostic evidence for the next implementation pass.
