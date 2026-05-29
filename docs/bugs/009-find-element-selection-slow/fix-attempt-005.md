# Fix Attempt 005

## Attempt Status
awaiting-user-confirmation

## Goal
Fix the remaining tree issue where **Find element on screen** and normal refreshed tree nodes can miss sub-components or fail to expose children after the lazy-loading changes.

## Relation To Previous Attempts
Attempt 004 introduced targeted lazy loading and placeholder nodes for finder-created proxy nodes. Review after the user's follow-up found that refresh-created top-level proxy nodes were still built manually without placeholders, so they could appear as leaves and hide sub-components.

## Proposed Change
- Reuse `CreateProxyNode` when applying refresh snapshots so refreshed top-level windows get the same lazy child placeholder as finder-created nodes.
- Preserve the refresh snapshot's precomputed text by assigning it after node creation.
- Keep the direct finder path and targeted lazy loading from attempt 004.

## Risks
- Leaf controls can show an expand glyph until expanded and found empty, which is the trade-off for avoiding eager subtree enumeration.

## Expected Verification
- Build with `.\build.ps1`.
- Run x64 artifact tests.
- Run x86 artifact tests.
- Copy updated `ManagedSpy.dll`/PDB into `artifacts\release` for the user's launch path if the default artifact rebuild remains locked by target processes.
- Ask the user to retest **Find element on screen**, tree completeness, and Layout diagram tweaks.

## Files Or Components Involved
- `ManagedSpy\MainForm.cs`
- `docs\bugs\009-find-element-selection-slow\*`

## Actual Implementation Summary
- `ApplyRefreshSnapshot` now creates refreshed top-level tree nodes via `CreateProxyNode`, then applies the refresh snapshot's display text.
- This keeps refreshed nodes and finder-created nodes on the same lazy-loading path, including child placeholders for expandable controls.

## Test And Verification Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt29-build` passed.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt29-build\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 17/17.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\attempt29-build\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 17/17.
- Copied updated `ManagedSpy.dll`/PDB into `artifacts\release\x86` and `artifacts\release\x64`; stopped the specific locked `artifacts\release\x86\ManagedSpy.exe` process before copying x86.

## Outcome And Remaining Gaps
- Awaiting user confirmation that **Find element on screen** finds/selects the correct tree node quickly and that refreshed tree nodes expose sub-components correctly.
