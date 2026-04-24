# Initial Findings

## Confirmed Facts
- `ManagedSpyLib.dll` is now built as C++/CLI targeting `net10.0`.
- ManagedSpy injects this DLL into target processes via Windows hooking.
- The reported exception comes from in-memory managed module loading inside target process runtime context.
- Current `Desktop::IsManagedProcess` treats both .NET Framework (`clr.dll`/`mscorlib`) and modern .NET (`coreclr.dll`/`System.Private.CoreLib`) as eligible targets.

## Likely Cause
Runtime compatibility is not enforced before hook injection. The .NET 10 hook assembly is injected into managed processes running other runtimes, which cannot satisfy `System.Runtime, Version=10.0.0.0`, causing target-process load failure and crash.

## Unknowns
- Exact runtime-version boundary for compatibility (assumed .NET 10+).
- Whether specific deployment models (self-contained/single-file) need extra compatibility handling.

## Reproduction Status
- User-provided stack trace confirms failure mode.
- Code-path inspection confirms injection can occur for non-.NET-10 managed processes.

## Evidence Gathered
- User exception stack trace from `AssemblyLoadContext.LoadFromInMemoryModule`.
- `ManagedSpyLib\Commands.cpp` current eligibility logic in `Desktop::IsManagedProcess`.
