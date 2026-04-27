# ManagedSpy — Comprehensive Code Review

**Date:** 2026-04-27
**Scope:** Full codebase review covering architecture, security, reliability, memory management, testing, and UX.
**Reviewer:** Automated deep review

---

## Executive Summary

ManagedSpy is a Windows desktop tool that inspects live .NET WinForms applications by injecting a DLL hook into target processes and communicating via shared memory (memory-mapped files) and custom registered window messages. The architecture is inherently risky — cross-process code injection, BinaryFormatter deserialization, and shared mutable state — but the codebase handles many edge cases competently.

**Key risks identified:**
1. **BinaryFormatter deserialization** in cross-process communication is a critical security surface.
2. **Shared memory race conditions** between the spy and spied processes.
3. **Resource leaks** in native code paths (Process handles, MemoryStore lifetime).
4. **Limited test coverage** — only `ScreenBoundsHelper` has automated tests.

The recent modernization to .NET 10 and overlay-based highlighting are positive steps. The bug-fix history (23 attempts on bug #003) reveals how difficult cross-process UI geometry is to get right.

---

## Architecture Overview

```
┌─────────────────────┐         SetWindowsHookEx           ┌──────────────────────┐
│   ManagedSpy.exe    │ ──────────────────────────────────►│  Target Process      │
│   (C# WinForms)     │         WM_* messages              │  (injected DLL)      │
│                     │◄────────────────────────────────── │                      │
│  - MainForm         │    Shared memory (MemoryStore)     │  - ManagedSpyLib.dll │
│  - PropertyGrid     │◄──────────────────────────────────►│  - ControlProxy      │
│  - EventGrid        │                                    │  - MessageHookProc   │
└─────────────────────┘                                    └──────────────────────┘
```

**Key components:**
- `Commands.cpp` — Hook installation, message dispatch, event subscription
- `ControlProxy.cpp/.h` — Proxy object serialized across process boundaries
- `Mem.cpp/.h` — Cross-process shared memory transport via `CAtlFileMapping`
- `ScreenBoundsHelper.cpp/.h` — DPI-aware control bounds calculation
- `MainForm.cs` — UI with tree navigation, element finder, persistent highlight

---

## Findings

### 1. Security & Safety

#### 1.1 BinaryFormatter Deserialization (Critical)

**Severity:** Critical
**Files:** `ManagedSpyLib\Mem.cpp:102-104, 139-143`

**Evidence:**
```cpp
BinaryFormatter^ formatter = gcnew BinaryFormatter();
formatter->Serialize(stream, data);
// ...
retvalue = formatter->Deserialize(stream);
```

**Risk:** `BinaryFormatter` is a known insecure deserialization vector. Any process that can write to the shared memory mapping names (`MSFT_ManagedSpy_PARAMS.*`, `MSFT_ManagedSpy_RETVAL.*`) can craft a malicious payload to achieve remote code execution in ManagedSpy or any hooked process.

The project explicitly enables this: `<EnableUnsafeBinaryFormatterSerialization>true</EnableUnsafeBinaryFormatterSerialization>` in `ManagedSpy.csproj:12`.

**Recommendation:** Migrate to a restricted serializer (e.g., `System.Text.Json` with known type allowlists, or a custom binary protocol for the small set of types actually exchanged). At minimum, implement a `SerializationBinder` that whitelists only expected types.

---

#### 1.2 Shared Memory Names Are Predictable (High)

**Severity:** High
**Files:** `ManagedSpyLib\Mem.cpp:88-89`

**Evidence:**
```cpp
m_paramKey.Format(_T("MSFT_ManagedSpy_PARAMS.%d.%d"), processId, transactionId);
m_retvalKey.Format(_T("MSFT_ManagedSpy_RETVAL.%d.%d"), processId, transactionId);
```

**Risk:** Any local process can guess/enumerate these names and read/write shared memory sections. Combined with BinaryFormatter deserialization, this allows local privilege escalation if ManagedSpy or the target runs elevated.

**Recommendation:** Use randomized names (GUIDs) or restrict the security descriptor on the file mapping to only allow access from the spy and target process SIDs.

---

#### 1.3 ChangeWindowMessageFilter Allows All Sources (Medium)

**Severity:** Medium
**Files:** `ManagedSpyLib\MessageFilters.cpp:7-19`

**Evidence:**
```cpp
ChangeWindowMessageFilter(WM_GETPROXY, MSGFLT_ADD);
ChangeWindowMessageFilter(WM_SETMGDPROPERTY, MSGFLT_ADD);
```

**Risk:** `ChangeWindowMessageFilter` with `MSGFLT_ADD` allows any lower-integrity process to send these registered messages to ManagedSpy's window. A malicious app could send crafted messages to trigger property sets on target applications.

**Recommendation:** Use `ChangeWindowMessageFilterEx` scoped to specific windows, and consider whether UIPI bypass is truly needed for the event-target window.

---

#### 1.4 PROCESS_ALL_ACCESS Used for Bitness Check (Medium)

**Severity:** Medium
**Files:** `ManagedSpyLib\Commands.cpp:163`

**Evidence:**
```cpp
auto handle = OpenProcess(PROCESS_ALL_ACCESS, FALSE, processID);
```

**Risk:** Requesting `PROCESS_ALL_ACCESS` is overly broad and will fail on protected processes. It also trips security tooling alerts.

**Recommendation:** Use `PROCESS_QUERY_LIMITED_INFORMATION` which is sufficient for `IsWow64Process`.

---

### 2. Reliability & Correctness

#### 2.1 Hook Crashes Target Apps (Known Bug — Partially Mitigated)

**Severity:** High
**Files:** `ManagedSpyLib\Commands.cpp:92-103, 189-278`
**Bug:** `docs\bugs\001-hook-crashes-target-apps`

The runtime compatibility check (`IsManagedProcess`) now guards against injecting into incompatible .NET runtimes. However, the check requires `>= 10` major version — future .NET versions with breaking changes could still cause crashes.

**Recommendation:** Add a version ceiling check or capability probe before hook injection. Consider a try/catch in the hook entry point for graceful degradation.

---

#### 2.2 FlashWindowHandle Blocks UI Thread (Medium)

**Severity:** Medium
**Files:** `ManagedSpy\MainForm.cs:1129-1136`

**Evidence:**
```csharp
for (int i = 0; i < 5; i++) {
    highlightOverlay.ShowHighlight(rectangle);
    Thread.Sleep(80);
    highlightOverlay.HideHighlight();
    Thread.Sleep(60);
}
```

**Risk:** 700ms of `Thread.Sleep` on the UI thread freezes the application. If the target window moves during the flash, the highlight stays in the old position.

**Recommendation:** Use an async timer or `Task.Delay` pattern to avoid blocking the message pump.

---

#### 2.3 SendMessage Deadlock Risk (High)

**Severity:** High
**Files:** `ManagedSpyLib\Commands.cpp:112-138`, `ManagedSpyLib\Mem.cpp:20`

**Evidence:**
```cpp
::SendMessage((HWND)m_notificationWindow.ToPointer(), Msg, m_processId, m_transactionId);
```

**Risk:** `SendMessage` is synchronous and cross-thread/cross-process. If the target's UI thread is blocked (e.g., showing a modal dialog, in a deadlock, or waiting on ManagedSpy), this creates a classic cross-process `SendMessage` deadlock.

**Recommendation:** Consider using `SendMessageTimeout` with `SMTO_ABORTIFHUNG` to prevent indefinite hangs, or switch to `PostMessage` with a completion callback mechanism.

---

#### 2.4 Static Mutable State in Desktop Class (Medium)

**Severity:** Medium
**Files:** `ManagedSpyLib\Commands.h:47-58`

**Evidence:**
```cpp
static List<ControlProxy^>^ topLevelWindows = gcnew List<ControlProxy^>();
static List<ControlProxy^>^ childWindows = gcnew List<ControlProxy^>();
```

**Risk:** `topLevelWindows` and `childWindows` are used as scratch buffers during enumeration. If enumeration is triggered concurrently (e.g., from the event target window receiving messages while `GetTopLevelWindows()` is running), data corruption occurs.

**Recommendation:** Use local lists instead of static scratch buffers, or protect with synchronization.

---

#### 2.5 Missing null check in treeWindow_AfterSelect (Low)

**Severity:** Low
**Files:** `ManagedSpy\MainForm.cs:1055`

**Evidence:**
```csharp
this.propertyGrid.SelectedObject = this.treeWindow.SelectedNode.Tag;
```

**Risk:** If `SelectedNode` is null (possible during programmatic tree manipulation), this throws a `NullReferenceException`.

**Recommendation:** Add a null guard.

---

### 3. Memory Management & Resource Leaks

#### 3.1 Process Object Leaks (Medium)

**Severity:** Medium
**Files:** `ManagedSpyLib\ControlProxy.h:196-207`, `ManagedSpyLib\Commands.cpp:185`

**Evidence:**
```cpp
Process^ get() {
    DWORD procid;
    GetWindowThreadProcessId((HWND)Handle.ToPointer(), &procid);
    Process^ owningprocess = nullptr;
    owningprocess = Process::GetProcessById(procid);
    return owningprocess;
}
```

**Risk:** `Process::GetProcessById` returns a new `Process` instance with an open native handle each time it is called. The `OwningProcess` property getter creates a new Process object on every access without disposing it.

**Recommendation:** Cache the process or call `Dispose()` on process objects after use. Consider using `using` statements at call sites.

---

#### 3.2 MemoryStore Leak When CreateStore Fails Mid-Operation (Low)

**Severity:** Low
**Files:** `ManagedSpyLib\Mem.cpp:60-84`

**Evidence:**
```cpp
newStore = new MemoryStore((int)GetCurrentProcessId(), nextTid, notificationWindow);
processMap[nextTid] = newStore;
```

If `StoreData` subsequently fails in `SendDataMessage`, the store is still properly deleted in the `finally` block of `SendMarshaledMessage`. However, the transaction ID linear scan (`while (processMap.Lookup(nextTid, store) && nextTid < MAXTID)`) has O(n) worst-case behavior — repeated failures could exhaust the 999-transaction limit.

**Recommendation:** Use a free-list or atomic counter instead of linear scanning.

---

#### 3.3 HelpAbout Dialog Not Disposed (Low)

**Severity:** Low
**Files:** `ManagedSpy\MainForm.cs:1209-1211`

**Evidence:**
```csharp
HelpAbout about = new HelpAbout();
about.ShowDialog();
```

**Risk:** `ShowDialog()` callers should dispose the form to release GDI handles.

**Recommendation:** Wrap in `using`.

---

### 4. Cross-Process Communication Risks

#### 4.1 No Versioning on Wire Protocol (Medium)

**Severity:** Medium
**Files:** `ManagedSpyLib\Messages.h:1-28`

**Risk:** The registered window messages and shared memory format have no version negotiation. If ManagedSpy and the hooked DLL get out of sync (e.g., partial update), deserialization will silently fail or produce garbage.

**Recommendation:** Add a protocol version header to the `SharedData` structure.

---

#### 4.2 Hook Re-enabled During Cleanup (Medium)

**Severity:** Medium
**Files:** `ManagedSpyLib\Commands.cpp:129-131`

**Evidence:**
```cpp
if (hookRequired) {
    Desktop::EnableHook(hWnd);  // enable it during deletion for memory release messages.
}
delete store;
```

**Risk:** Re-enabling the hook after `SendMessage` but before `delete store` means the target process might receive new messages while shared memory is being torn down. A race between teardown and a new operation could crash.

**Recommendation:** Document this invariant clearly or use a reference-counted hook lifetime.

---

#### 4.3 Assembly Loading from Arbitrary Paths (Medium)

**Severity:** Medium
**Files:** `ManagedSpyLib\ControlProxy.cpp:237-246`

**Evidence:**
```cpp
assemblies->Add(Assembly::LoadFile(assemblyPaths[i]));
```

**Risk:** The assembly paths come from the target process (serialized in the ControlProxy). A compromised target could provide paths to malicious assemblies that get loaded into ManagedSpy's process.

**Recommendation:** Validate assembly paths against known safe locations or verify assembly signatures before loading.

---

### 5. Testing Gaps

#### 5.1 Only ScreenBoundsHelper Has Tests (High)

**Severity:** High
**Files:** `ManagedSpy.Tests\ScreenBoundsHelperTests.cs`

**Gap:** The only automated tests cover `ScreenBoundsHelper`. The core cross-process machinery (`Commands`, `MemoryStore`, `ControlProxy`, `PropertyDescriptorProxy`, `EventTargetWindow`) has zero test coverage.

**Recommendation:** Add integration tests that spawn a known target process and exercise the hook/proxy lifecycle. Unit test `MemoryStore` serialization round-trips. Mock-based tests for `PropertyDescriptorProxy`.

---

#### 5.2 No Negative/Error Path Tests (Medium)

**Severity:** Medium

**Gap:** No tests exercise:
- What happens when target process exits mid-communication
- What happens when shared memory names collide
- What happens when hook injection fails
- What happens when deserialization throws

**Recommendation:** Add fault-injection tests for the critical communication paths.

---

### 6. Maintainability & Code Quality

#### 6.1 Large MainForm.cs File (~1350 lines) (Medium)

**Severity:** Medium
**Files:** `ManagedSpy\MainForm.cs`

The file mixes element finder logic, persistent highlight logic, tree management, event logging, property application, and overlay form classes in a single file.

**Recommendation:** Extract `HighlightOverlayForm` and `ClickToolStrip` into separate files. Consider extracting element finder and persistent highlight into dedicated controller classes.

---

#### 6.2 Namespace Inconsistency (Low)

**Severity:** Low
**Files:** `ManagedSpy\NativeMethods.cs:7` (namespace `MSpy`), `ManagedSpy\MainForm.cs:12` (namespace `ManagedSpy`)

**Risk:** `NativeMethods.cs` uses `namespace MSpy` while the rest of the project uses `namespace ManagedSpy`. This suggests the file may be dead code or improperly integrated.

**Recommendation:** Verify if `NativeMethods.cs` is actually used. If so, unify the namespace.

---

#### 6.3 Catch-All Exception Swallowing (Medium)

**Severity:** Medium
**Files:** `ManagedSpyLib\Commands.cpp:342, 390`

**Evidence:**
```cpp
catch(...) {}
```

**Risk:** Silently swallowing all exceptions in `MessageHookProc` and `GetEventHandler` hides bugs and makes debugging extremely difficult.

**Recommendation:** At minimum log to debug output. Consider catching only expected exception types.

---

#### 6.4 Bug #003 Required 23 Fix Attempts (Observation)

**Severity:** N/A (process observation)
**Files:** `docs\bugs\003-tree-subtree-refresh-highlight`

The persistent highlight feature required 23 iterative fix attempts, indicating the cross-process geometry problem is inherently complex and under-specified. The final fix (recreating the overlay window on target switches) suggests Windows compositor caching issues that are difficult to unit-test.

**Recommendation:** Document the "recreate overlay on switch" requirement as an architectural invariant with a comment explaining why.

---

### 7. UX Issues

#### 7.1 Diagnostics Log Written to Application Directory (Low)

**Severity:** Low
**Files:** `ManagedSpy\MainForm.cs:57-59`

**Evidence:**
```csharp
private static readonly string persistentHighlightDiagnosticsPath = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "ManagedSpy-highlight-diagnostics.log");
```

**Risk:** Writing to the application directory may fail if installed in Program Files. The log grows without bounds.

**Recommendation:** Write to `%LOCALAPPDATA%\ManagedSpy\` or make diagnostic logging opt-in with log rotation.

---

#### 7.2 No Feedback When Target Process Is Incompatible (Low)

**Severity:** Low
**Files:** `ManagedSpyLib\Commands.cpp:268-278`

When a process is detected as incompatible (.NET Framework or old .NET), it's silently added to `unmanagedProcesses` with no user feedback. Users may wonder why an application doesn't appear in the tree.

**Recommendation:** Add a status bar message or filtered-out indicator when known managed processes are skipped due to version incompatibility.

---

### 8. Platform/Compatibility Risks

#### 8.1 x86/x64 Bitness Must Match (By Design, But Undiscoverable)

**Severity:** Low
**Files:** `ManagedSpyLib\Commands.cpp:156-183`

The bitness check is correct but the failure mode is silent. Users running x64 ManagedSpy won't see x86 target processes and vice versa.

**Recommendation:** Display filtered processes with a "wrong architecture" indicator rather than hiding them completely.

---

#### 8.2 DPI-Awareness Assumptions (Medium)

**Severity:** Medium
**Files:** `ManagedSpyLib\ScreenBoundsHelper.cpp:78-100`

The `LogicalToPhysicalPointForPerMonitorDPI` call assumes per-monitor DPI awareness. If ManagedSpy or the target process has different DPI awareness modes, coordinate conversions may be incorrect.

**Recommendation:** Query the DPI awareness context of both processes before applying transformations. Consider declaring per-monitor-v2 DPI awareness in the application manifest.

---

#### 8.3 LoadLibrary with Unqualified Path (Medium)

**Severity:** Medium
**Files:** `ManagedSpyLib\Commands.cpp:95`

**Evidence:**
```cpp
hinstDLL = LoadLibrary((LPCTSTR) _T("ManagedSpyLib.dll"));
```

**Risk:** Using an unqualified DLL name is subject to DLL search order hijacking. If a malicious `ManagedSpyLib.dll` is placed in the target process's working directory, it could be loaded instead.

**Recommendation:** Use a fully-qualified path to the DLL, or use `LoadLibraryEx` with `LOAD_LIBRARY_SEARCH_APPLICATION_DIR`.

---

## Strengths

1. **Modern .NET 10 target** — The migration from legacy .NET Framework is well-executed with proper runtime version gating.
2. **Overlay-based highlighting** — The switch from XOR-based drawing to overlay windows is a correct architectural improvement that eliminates artifact issues.
3. **Robust error handling in IsManagedProcess** — Multiple exception types are caught with proper fallback to the unmanaged list.
4. **ScreenBoundsHelper is well-tested** — The 4 test cases cover top-level, nested, clipped, and accessibility scenarios with proper STA thread isolation.
5. **Clean build infrastructure** — `build.ps1` properly resolves MSBuild, handles both platforms, and provides good error messages.
6. **Careful handle lifecycle management** — The `HandleCreated`/`HandleDestroyed` event subscriptions and proxy cache updates show attention to handle recycling.
7. **Event target window design** — Separating the event-receiving window from the main form is clean architecture.
8. **Bug documentation** — The structured bug tracking with numbered attempts provides excellent audit trail.

---

## Prioritized Action List

| # | Priority | Action | Effort |
|---|----------|--------|--------|
| 1 | **Critical** | Replace BinaryFormatter with a type-restricted serializer or implement a strict SerializationBinder | High |
| 2 | **High** | Use `SendMessageTimeout` with `SMTO_ABORTIFHUNG` instead of `SendMessage` for cross-process calls | Medium |
| 3 | **High** | Change `OpenProcess(PROCESS_ALL_ACCESS, ...)` to `PROCESS_QUERY_LIMITED_INFORMATION` | Low |
| 4 | **High** | Add integration tests for the hook/proxy lifecycle (spawn target, exercise protocol) | High |
| 5 | **High** | Use fully-qualified path in `LoadLibrary` call to prevent DLL hijacking | Low |
| 6 | **Medium** | Randomize shared memory names or add security descriptors to file mappings | Medium |
| 7 | **Medium** | Replace `Thread.Sleep` in `FlashWindowHandle` with async timer | Low |
| 8 | **Medium** | Make static `childWindows`/`topLevelWindows` thread-safe or use locals | Low |
| 9 | **Medium** | Cache or dispose `Process` objects from `OwningProcess` property | Low |
| 10 | **Medium** | Add protocol versioning to `SharedData` structure | Medium |
| 11 | **Medium** | Use `ChangeWindowMessageFilterEx` instead of process-wide `ChangeWindowMessageFilter` | Low |
| 12 | **Medium** | Extract `HighlightOverlayForm`, element finder, and persistent highlight into separate files | Medium |
| 13 | **Low** | Move diagnostics log to `%LOCALAPPDATA%` with opt-in and rotation | Low |
| 14 | **Low** | Validate assembly paths before `Assembly::LoadFile` | Medium |
| 15 | **Low** | Unify namespace in `NativeMethods.cs` or remove if unused | Low |
| 16 | **Low** | Add null guards in `treeWindow_AfterSelect` | Low |
| 17 | **Low** | Wrap `HelpAbout` dialog in `using` statement | Low |

---

*End of review.*
