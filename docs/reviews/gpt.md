# ManagedSpy — Deep Code Review

**Date:** 2026-04-27  
**Scope:** Full implementation review of the WinForms app, native interop library, build scripts, and tests.  
**Method:** Direct code inspection plus build/test validation.

## Executive Summary

ManagedSpy has some strong recent work in high-friction areas such as screen-bounds calculation, subtree refresh behavior, and overlay rendering, but its core architecture still carries several high-risk weaknesses. The most important problems are concentrated in the cross-process transport layer: unsafe deserialization, predictable IPC naming, target-controlled assembly loading, synchronous `SendMessage` usage, and weak validation around event delivery. Those risks are amplified by thin automated coverage and a build/test path that is not yet reliable for CI.

The project is viable, but today it is best treated as a powerful diagnostic tool for trusted local targets rather than a hardened inspector. If you want it to be robust against unusual apps, hung apps, or hostile local processes, the IPC boundary needs the most attention.

## Validation Snapshot

- `build.ps1 -Platform x64 -Configuration Release` succeeded.
- `dotnet test ManagedSpy.Tests\ManagedSpy.Tests.csproj -c Debug` failed with `MSB4278` because the test project references a native `.vcxproj` and the `dotnet` build path cannot import C++ targets.
- `dotnet vstest artifacts\release\x64\ManagedSpy.Tests.dll /Platform:x64` passed `4/4` tests.

## Strengths

- [ManagedSpyLib\ScreenBoundsHelper.cpp](../../ManagedSpyLib/ScreenBoundsHelper.cpp) is thoughtful about accessibility bounds, client bounds, clipping, and DPI normalization, and [ManagedSpy.Tests\ScreenBoundsHelperTests.cs](../../ManagedSpy.Tests/ScreenBoundsHelperTests.cs) covers the main success cases.
- [ManagedSpy\MainForm.cs](../../ManagedSpy/MainForm.cs) preserves selection and expansion state during subtree refresh (`RefreshSubtree`, lines 290-329), which is exactly the kind of UX detail that matters in an inspector.
- [ManagedSpyLib\Commands.cpp](../../ManagedSpyLib/Commands.cpp) now explicitly distinguishes incompatible .NET Framework targets from compatible modern .NET targets (`IsManagedProcess`, lines 189-278) instead of blindly attempting to hook everything.
- [ManagedSpy\MainForm.cs](../../ManagedSpy/MainForm.cs) isolates overlay behavior into `HighlightOverlayForm` (lines 1223-1335) with non-activating, click-through window styles, which is a clean design choice for screen highlighting.

## Findings Summary

| Severity | Area                    | Finding                                              | Primary references |
| :---     | :---                    | :---                                                 | :---               |
| Critical | Security / IPC          | Unsafe deserialization on spoofable cross-process IPC | [ManagedSpyLib\Mem.cpp](../../ManagedSpyLib/Mem.cpp), [ManagedSpy\ManagedSpy.csproj](../../ManagedSpy/ManagedSpy.csproj), [ManagedSpyLib\Messages.h](../../ManagedSpyLib/Messages.h) |
| High     | Security / Reliability  | Target-controlled assembly loading into the spy       | [ManagedSpyLib\ControlProxy.cpp](../../ManagedSpyLib/ControlProxy.cpp), [ManagedSpyLib\ControlProxy.h](../../ManagedSpyLib/ControlProxy.h) |
| High     | Reliability / UX        | Synchronous `SendMessage` IPC can freeze the UI       | [ManagedSpyLib\Mem.cpp](../../ManagedSpyLib/Mem.cpp), [ManagedSpyLib\Commands.cpp](../../ManagedSpyLib/Commands.cpp), [ManagedSpy\MainForm.cs](../../ManagedSpy/MainForm.cs) |
| High     | Correctness             | Event delivery path has null/range/order hazards      | [ManagedSpy\MainForm.cs](../../ManagedSpy/MainForm.cs), [ManagedSpyLib\EventTargetWindow.cpp](../../ManagedSpyLib/EventTargetWindow.cpp), [ManagedSpyLib\ControlProxy.h](../../ManagedSpyLib/ControlProxy.h) |
| Medium   | Reliability             | Process/runtime detection cache is brittle over time  | [ManagedSpyLib\Commands.h](../../ManagedSpyLib/Commands.h), [ManagedSpyLib\Commands.cpp](../../ManagedSpyLib/Commands.cpp), [ManagedSpy\MainForm.cs](../../ManagedSpy/MainForm.cs) |
| Medium   | UX / Performance        | Live event logging has no backpressure                | [ManagedSpy\EventFilterDialog.cs](../../ManagedSpy/EventFilterDialog.cs), [ManagedSpy\MainForm.cs](../../ManagedSpy/MainForm.cs), [ManagedSpy\MainForm.Designer.cs](../../ManagedSpy/MainForm.Designer.cs) |
| Medium   | Testing / Release       | Validation pipeline is thin and partially broken      | [build.ps1](../../build.ps1), [ManagedSpy.Tests\ManagedSpy.Tests.csproj](../../ManagedSpy.Tests/ManagedSpy.Tests.csproj), [scripts\Prepare-Release.ps1](../../scripts/Prepare-Release.ps1) |
| Low      | UX / Feature coverage   | Event filter UI does not truly represent target events | [ManagedSpy\EventFilterDialog.cs](../../ManagedSpy/EventFilterDialog.cs) |

## Detailed Findings

### 1. Critical — Unsafe deserialization on a spoofable IPC boundary

**Evidence**

- [ManagedSpyLib\Mem.cpp](../../ManagedSpyLib/Mem.cpp) lines 97-149 serialize and deserialize cross-process payloads with `BinaryFormatter`.
- [ManagedSpy\ManagedSpy.csproj](../../ManagedSpy/ManagedSpy.csproj) line 12 explicitly enables unsafe BinaryFormatter support and line 24 adds the formatter package.
- [ManagedSpyLib\Mem.cpp](../../ManagedSpyLib/Mem.cpp) lines 87-94 derive mapping names from predictable `processId` and `transactionId` values.
- [ManagedSpyLib\Messages.h](../../ManagedSpyLib/Messages.h) lines 4-28 registers fixed global message names, and [ManagedSpyLib\MessageFilters.cpp](../../ManagedSpyLib/MessageFilters.cpp) lines 5-20 broadens the process-wide message filter for them.

**Why this matters**

This is the most serious issue in the repository. A local process that can guess or observe the mapping/message names can interfere with the transport, inject unexpected payloads, or feed serialized objects into ManagedSpy's deserializer. Because this is a desktop inspection tool, the most realistic threat is not internet-originated input; it is hostile or compromised local software running in the same session. In that threat model, the current IPC design is much too trusting.

**Recommendation**

- Replace `BinaryFormatter` with a strict allowlisted transport.
- Prefer a tiny custom protocol or a serializer over explicit DTOs only.
- Randomize mapping names per transaction and apply restrictive security descriptors.
- Narrow message acceptance to the specific receiving window and validate the sender before acting.

### 2. High — The target can influence what assemblies the spy loads

**Evidence**

- [ManagedSpyLib\ControlProxy.cpp](../../ManagedSpyLib/ControlProxy.cpp) lines 73-77 enumerate every assembly in the target AppDomain and persist each assembly `Location`.
- [ManagedSpyLib\ControlProxy.cpp](../../ManagedSpyLib/ControlProxy.cpp) lines 233-248 later call `Assembly::LoadFile` on those captured paths inside the spying process.
- [ManagedSpyLib\ControlProxy.h](../../ManagedSpyLib/ControlProxy.h) lines 355-374 installs an `AssemblyResolve` handler that serves these loaded assemblies back during type resolution.

**Why this matters**

This is a security risk and a platform-specific reliability risk. From a security angle, inspecting a malicious target should not cause the inspector itself to load arbitrary assemblies from target-controlled paths. From a reliability angle, modern apps may include dynamic assemblies, bundled assemblies, or assemblies with unusual load semantics; `Assembly.Location` is not guaranteed to be a safe, stable file path for every assembly the target has loaded.

The result is that a hostile or just unusual target can either pull extra code into the spy process or make proxy materialization fail in hard-to-diagnose ways.

**Recommendation**

- Stop using `Assembly::LoadFile` on target-provided paths.
- Transfer only the metadata actually needed for display and editing.
- If type loading is unavoidable, restrict it to known framework/application assemblies and defensively handle empty, invalid, or non-file-backed locations.

### 3. High — Synchronous `SendMessage` IPC can hang ManagedSpy indefinitely

**Evidence**

- [ManagedSpyLib\Mem.cpp](../../ManagedSpyLib/Mem.cpp) line 20 sends the request with `::SendMessage(...)`.
- [ManagedSpyLib\Mem.cpp](../../ManagedSpyLib/Mem.cpp) line 56 also uses synchronous `SendMessage` during memory release.
- [ManagedSpyLib\Commands.cpp](../../ManagedSpyLib/Commands.cpp) lines 112-138 make this transport the default path for proxy creation, property access, screen-rectangle queries, and event subscription.
- [ManagedSpy\MainForm.cs](../../ManagedSpy/MainForm.cs) lines 1018-1048 refresh the tree on the UI thread, and lines 1141-1165 start/stop event logging on the UI thread.

**Why this matters**

If the target process is hung, blocked in its UI thread, or deadlocked around message handling, ManagedSpy will also block. This creates a poor debugger experience precisely when the user is most likely to reach for the tool: when a target is behaving badly. Because many UI actions in `MainForm` call into the transport synchronously, the failure mode is not isolated; the whole inspector can feel frozen.

**Recommendation**

- Move cross-process calls behind `SendMessageTimeout(..., SMTO_ABORTIFHUNG, ...)` or a comparable bounded wait.
- Run expensive or failure-prone remote operations off the UI thread.
- Surface timeout/failure state in the UI instead of silently hanging.

### 4. High — Event delivery has null, range, and ordering hazards

**Evidence**

- [ManagedSpy\MainForm.cs](../../ManagedSpy/MainForm.cs) lines 1141-1152 subscribe remote events before attaching the local `currentProxy.EventFired` handler.
- [ManagedSpyLib\EventTargetWindow.cpp](../../ManagedSpyLib/EventTargetWindow.cpp) line 23 checks `<= events->Count` before indexing the event collection, which still permits an out-of-range index at exactly `Count`.
- [ManagedSpyLib\ControlProxy.h](../../ManagedSpyLib/ControlProxy.h) lines 220-224 raise `EventFired` without a null check.

**Why this matters**

This is a real cross-file correctness bug. If a target event fires immediately after remote subscription but before the local handler is attached, `RaiseEvent` can run with no subscribers. Separately, malformed or stale event indices can slip through the `<=` check and hit invalid indexing. Either way, the event logging path is less robust than it looks, and failures in this path are especially painful because they are data-dependent and timing-dependent.

**Recommendation**

- Attach the local event handler before requesting remote subscriptions.
- Change the range check to `< events->Count`.
- Null-check `EventFired` before invocation.
- Add a focused automated test or harness for immediate-fire events and stale event IDs.

### 5. Medium — Process/runtime detection is brittle over long sessions and under restricted access

**Evidence**

- [ManagedSpyLib\Commands.h](../../ManagedSpyLib/Commands.h) lines 47-58 keep static `managedProcesses` and `unmanagedProcesses` lists for the life of the process.
- [ManagedSpyLib\Commands.cpp](../../ManagedSpyLib/Commands.cpp) lines 189-277 add process IDs to those caches but never age them out.
- [ManagedSpyLib\Commands.cpp](../../ManagedSpyLib/Commands.cpp) line 163 opens processes with `PROCESS_ALL_ACCESS` just to determine bitness.
- [ManagedSpy\MainForm.cs](../../ManagedSpy/MainForm.cs) lines 1028-1035 assume process metadata is still readable after `OwningProcess` succeeds.

**Why this matters**

PID-based caches without invalidation are vulnerable to PID reuse. Over a long-running session, a terminated process ID can later belong to a different process and inherit stale “managed” or “unmanaged” classification. `PROCESS_ALL_ACCESS` also increases the odds of false negatives on restricted or elevated targets when the code only needs query-level access for detection.

**Recommendation**

- Replace permanent PID caches with short-lived caches keyed by PID plus start time when possible.
- Use lower-privilege process access rights.
- Wrap post-lookup reads such as `ProcessName` and `MainWindowTitle` in defensive exception handling.

### 6. Medium — Live event logging can overwhelm the UI

**Evidence**

- [ManagedSpy\EventFilterDialog.cs](../../ManagedSpy/EventFilterDialog.cs) lines 61-66 enable every `Control` event by default.
- [ManagedSpy\MainForm.cs](../../ManagedSpy/MainForm.cs) lines 1065-1067 append a row for every event with no cap, batching, or sampling.
- [ManagedSpy\MainForm.Designer.cs](../../ManagedSpy/MainForm.Designer.cs) lines 165-190 configure the grid with `AllCells` auto-sizing.

**Why this matters**

Noisy controls can generate large event volumes very quickly. In the current design, each event creates a row and forces expensive grid layout behavior on the UI thread. That is a recipe for freezes, memory growth, and poor responsiveness, especially because the default filter is permissive.

**Recommendation**

- Default to a conservative event set.
- Add a maximum retained row count.
- Batch UI updates or suspend sizing while logging.
- Consider a text-buffer/list model optimized for append-heavy workloads.

### 7. Medium — The validation and release pipeline is not trustworthy yet

**Evidence**

- [build.ps1](../../build.ps1) line 66 builds the solution but never runs tests, and lines 89-90 always print a “Release build complete” style message regardless of configuration.
- [ManagedSpy.Tests\ManagedSpy.Tests.csproj](../../ManagedSpy.Tests/ManagedSpy.Tests.csproj) lines 28-30 reference the native project directly, which breaks the normal `dotnet test` path in this repo.
- [ManagedSpy.Tests\ScreenBoundsHelperTests.cs](../../ManagedSpy.Tests/ScreenBoundsHelperTests.cs) is the only test file, with only four tests focused on `ScreenBoundsHelper`.
- [scripts\Prepare-Release.ps1](../../scripts/Prepare-Release.ps1) line 26 does not pass the declared `$Solution`, and line 27 packages from `ManagedSpy\bin\...` rather than the current `artifacts\release\...` output layout.
- Repository scan found no checked-in workflow files under `.github\workflows`.
- [.gitignore](../../.gitignore) lines 12-16 ignore project-local `bin` and `obj` folders but not the top-level `artifacts` directory created by the build script.

**Why this matters**

The codebase currently has a mismatch between “how you build it”, “how you test it”, and “how you package it”. That makes regressions easier to miss and releases easier to mispackage. The gap is most severe in the interop layer, where the risk is highest and the automated coverage is thinnest.

**Recommendation**

- Make one documented validation path authoritative.
- Fold test execution into that path.
- Fix `Prepare-Release.ps1` to package current outputs.
- Add CI for at least build + test on a Windows agent.
- Add integration coverage for proxy creation, event subscription, property set/get, and hung-target behavior.

### 8. Low — Event filter UX does not accurately represent what the target can emit

**Evidence**

- [ManagedSpy\EventFilterDialog.cs](../../ManagedSpy/EventFilterDialog.cs) lines 13-15 explicitly note that the dialog does not really show all events.
- [ManagedSpy\EventFilterDialog.cs](../../ManagedSpy/EventFilterDialog.cs) lines 61-66 populate the filter from `TypeDescriptor.GetEvents(typeof(Control))` rather than the selected target type.
- [ManagedSpy\EventFilterDialog.cs](../../ManagedSpy/EventFilterDialog.cs) lines 69-77 collapse unknown events into a generic “Custom Events” bucket.

**Why this matters**

For a diagnostic tool, discoverability matters. If the selected control exposes interesting custom events, the current UI does not make them visible or configurable in a meaningful way. That limits the tool's usefulness on real applications with custom controls.

**Recommendation**

Drive the event filter from the selected proxy type instead of `typeof(Control)`, and show truly unknown/custom events individually rather than routing them all through one fallback bucket.

## Testing Gaps Worth Closing First

- End-to-end tests that start a small sample WinForms target, inspect it, and validate `GetProxy`, `GetScreenBounds`, property get/set, and event subscription.
- Failure-mode tests for hung or non-responsive targets so IPC timeout behavior is enforced.
- Tests for event logging order and null-subscriber safety.
- Tests for dynamic/custom control types to prove the proxy/type-loading path works without loading arbitrary assemblies into the spy.
- Release-script smoke tests so packaging cannot silently drift from the real output layout.

## Prioritized Action List

1. Replace `BinaryFormatter` transport and harden the IPC boundary (message validation, randomized mapping names, narrower message filters).
2. Remove target-driven `Assembly::LoadFile` behavior and redesign type metadata transfer so the target cannot control what code the spy loads.
3. Put time bounds around all cross-process `SendMessage` calls and move slow remote operations off the UI thread.
4. Fix the event delivery path: subscribe local handlers first, correct the bounds check, and null-check event invocation.
5. Repair the build/test/release workflow so one Windows CI path builds, tests, and packages the same outputs developers use locally.
6. Add integration tests for the native interop surface, not just `ScreenBoundsHelper`.
7. Add UI backpressure for live event logging and broaden the event filter so it reflects the selected control type.
