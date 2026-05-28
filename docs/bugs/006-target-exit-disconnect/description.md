# Bug Description

## Title
ManagedSpy does not disconnect cleanly when the inspected target exits

## Status
- fixed

## Reported Symptoms
- When the inspected target application exits, ManagedSpy still appears connected to it.
- ManagedSpy keeps handles on some target application files.
- Other applications cannot modify those files until ManagedSpy releases them or exits.

## Expected Behavior
- ManagedSpy should disconnect from an inspected target when the target process exits.
- Process tree nodes, selected properties, event logging, and persistent highlight state should not keep stale target state alive.
- Inspecting a target should not lock the target application's assemblies on disk after the target exits.

## Actual Behavior
- ManagedSpy caches `ControlProxy` instances and process nodes.
- `ControlProxy.ComponentType` loads target application assemblies with `Assembly.LoadFile`, which can keep file handles/locks in the ManagedSpy process.
- Process nodes are not explicitly removed/disposed when the target process exits.

## Reproduction Details
1. Start a target .NET application.
2. Inspect it with ManagedSpy.
3. Select target controls/properties so ManagedSpy resolves component metadata.
4. Exit the target application.
5. Try to modify or rebuild target application files.
6. Observe file access failures while ManagedSpy remains open.

## Affected Area
- `ManagedSpyLib\ControlProxy.cs`
  - target assembly metadata loading
  - proxy cache cleanup
- `ManagedSpyLib\Desktop.cs`
  - proxy cache ownership by process
- `ManagedSpy\MainForm.cs`
  - process tree tracking
  - event logging and persistent highlight cleanup on process exit

## Constraints
- Preserve property/event browsing for inspected targets.
- Avoid loading target assemblies in a way that locks target files on disk.
- Disconnect only the exited target process, not all inspected processes.

## Open Questions
- Whether byte-loaded target assemblies provide enough metadata fidelity for all currently supported property/event browsing scenarios.
- Whether the UI should automatically refresh or simply remove the exited process node.
