# Process Compatibility and Limitations

ManagedSpy can only provide full managed inspection when several Windows,
runtime, bitness, and target-application conditions are satisfied. If a condition
is not satisfied, a process may be hidden from the default tree or may appear
only as a native window when **Show Native Windows** is enabled.

## Compatibility summary

| Target process kind                          | Default result                  | Notes                                                                        |
| :---                                         | :---                            | :---                                                                         |
| .NET 10 Windows Forms, same bitness          | Full managed inspection         | Intended current target.                                                     |
| .NET 10 Windows Forms, different bitness     | Not inspectable by that build   | Use x86 ManagedSpy for x86 targets and x64 ManagedSpy for x64 targets.       |
| .NET Framework Windows Forms                 | Not treated as compatible       | Runtime modules are detected but rejected by the .NET 10 compatibility gate. |
| .NET 6, .NET 7, .NET 8, or .NET 9 WinForms   | Not treated as compatible       | The current compatibility gate requires runtime major version 10 or later.   |
| WPF, WinUI, UWP, console, service processes  | No full managed control model   | ManagedSpy is Windows Forms-oriented; native windows may still be visible.   |
| Native Win32 applications                    | Native fallback only            | Visible only when **Show Native Windows** is enabled.                        |
| Elevated process from non-elevated inspector | Usually inaccessible            | Windows access checks and UIPI can prevent inspection.                       |
| Hung or blocked target process               | Partial or failed inspection    | Message timeouts avoid indefinite hangs but requests may return no data.     |

## Bitness rules

ManagedSpy is built separately for x86 and x64. The inspector process must match
the target process architecture:

- `artifacts\release\x86\ManagedSpy.exe` inspects 32-bit target processes.
- `artifacts\release\x64\ManagedSpy.exe` inspects 64-bit target processes.

On a 64-bit operating system, the x86 inspector uses `IsWow64Process` to avoid
inspecting non-WOW64 targets. The x64 inspector can inspect non-WOW64 64-bit
targets. Cross-bitness hook injection is not supported by this design.

## Runtime compatibility rules

Before attempting managed inspection, ManagedSpy enumerates process modules:

- .NET Framework modules such as `clr.dll`, `mscorlib.dll`, and
  `mscorlib.ni.dll` mark the process as managed but incompatible.
- Modern .NET modules such as `coreclr.dll`, `System.Private.CoreLib.dll`, and
  `System.Runtime.dll` mark the process as managed.
- A modern .NET process is considered compatible only when the detected runtime
  module file major version is `10` or newer.

This prevents the .NET 10 hook assembly from being injected into older managed
runtimes that cannot load its dependencies.

## Windows Forms requirement

ManagedSpy obtains rich managed data by executing code in the target process and
calling `Control.FromHandle`. This means full inspection requires the target
window handle to map to a `System.Windows.Forms.Control`.

For a compatible Windows Forms target, ManagedSpy can inspect:

- Control names and class names.
- Managed child controls.
- Type descriptor properties.
- Type descriptor events.
- Screen bounds calculated by Windows Forms and accessibility APIs.

For non-Windows Forms windows, ManagedSpy can still create a native fallback
proxy from the window handle and class name, but managed properties and events
are not available.

## Serialization and dependency requirements

The current IPC payload channel uses `BinaryFormatter` through
`System.Runtime.Serialization.Formatters`. Because the hook runs inside the
target process, the target process must be able to load the hook-side managed
dependencies and must allow the current serialization path.

The bundled [ManagedSpy.TestTarget](../../ManagedSpy.TestTarget/) is configured
as a known compatible target. Arbitrary .NET 10 Windows Forms applications may
still fail managed inspection if their runtime configuration prevents the current
serialization path from operating.

This is an implementation limitation, not a product goal. Replacing the IPC
payload serializer with a constrained protocol is the preferred long-term
hardening direction.

## Access and security boundaries

ManagedSpy does not bypass Windows process isolation. Inspection can fail when:

- The target process belongs to another desktop or session.
- The target process has a higher integrity level than ManagedSpy.
- The user account cannot open the target process with the requested access.
- Module enumeration is denied.
- UIPI blocks required window messages.
- Security software prevents hook injection or memory-mapped IPC.

Running ManagedSpy elevated can help when inspecting elevated targets, but it
also increases the blast radius of injected inspection code and should be done
only when needed.

## Caching behavior

ManagedSpy caches process IDs that are classified as managed or unmanaged during
the current session. This reduces repeated module scans, but it also means a
process classification can stay cached until ManagedSpy restarts. Process ID
reuse or target runtime changes after startup can therefore require restarting
ManagedSpy to force a clean classification.

## Operational limitations

- Only top-level windows discovered by `EnumWindows` are grouped initially.
- Child controls are expanded lazily as tree nodes are expanded.
- Property writes depend on `PropertyDescriptor` support and type conversion.
- Read-only properties cannot be changed.
- Some property values or event arguments cannot be serialized.
- Screen bounds can be affected by DPI, accessibility metadata, or target-window
  behavior.
- Highlight overlays are visual aids, not pixel-perfect automated assertions.
- A crashed or closed target can leave stale tree nodes until refresh.

