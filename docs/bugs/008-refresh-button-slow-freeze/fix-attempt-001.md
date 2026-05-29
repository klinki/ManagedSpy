# Fix Attempt 001

## Attempt Status
fixed

## Goal
Make Refresh responsive and remove the newly expensive managed-filter path.

## Relation To Previous Attempts
First attempt for this bug.

## Proposed Change
- Reorder `ControlProxy.IsManaged` so a known serialized managed proxy does not force `ComponentType` resolution.
- Add a cheap managed-proxy flag and use it in refresh filtering.
- Move expensive top-level window discovery and process metadata reads to a background task.
- Apply only the final tree update and process-exit tracking on the UI thread.
- Disable refresh commands while a refresh is already running.

## Risks
- Static proxy/process caches were previously UI-thread-only in practice; background discovery requires safe cache access.
- Target lifecycle events can occur while a background refresh is in progress.
- The tree must not be mutated from the background task.

## Files And Components
- `ManagedSpy\MainForm.cs`
- `ManagedSpyLib\ControlProxy.cs`
- `ManagedSpyLib\Desktop.cs`
- `docs\bugs\008-refresh-button-slow-freeze\*`

## Verification Plan
- Build with `.\build.ps1`.
- Run x64 artifact tests.
- Run x86 artifact tests.
- Ask the user to verify Refresh responsiveness and speed in their process environment.

## Implementation Summary
- Added `ControlProxy.IsKnownManagedProxy` and changed `IsManaged` to use already-serialized managed metadata before falling back to managed-process probing, avoiding `ComponentType` and byte-loaded assembly/type resolution during refresh filtering.
- Moved top-level window discovery, managed/native filtering, process metadata reads, and top-level node text collection into a background `Task`.
- Kept tree mutation, layout-tab clearing, and process-exit tracking on the UI thread by applying a completed refresh snapshot.
- Disabled the toolbar/menu Refresh commands while a refresh is running and reused the in-flight task for duplicate refresh requests.
- Added thread-safe access around the shared proxy cache and managed/unmanaged process lists that background refresh can now touch.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\refresh-layout-build-final` passed.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\refresh-layout-build-final\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 17/17.
- `dotnet vstest C:\Users\david\.copilot\session-state\83066827-1c20-4651-92ef-68b57e235073\files\refresh-layout-build-final\x86\ManagedSpy.Tests.dll /Platform:x86` passed: 17/17.

## Outcome
- Local implementation and automated validation completed successfully.
- User confirmed Refresh no longer freezes and is much better on 2026-05-29.

## Next Step
- Ask the user to retest Refresh responsiveness and speed.

## Remaining Gaps
- None for the reported Refresh freeze/slowdown.
