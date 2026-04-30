# Features and Workflows

This document describes what ManagedSpy exposes in the UI and how each workflow
maps to the current implementation.

## Main window layout

The main window is implemented in [ManagedSpy\MainForm.cs](../../ManagedSpy/MainForm.cs).
It contains:

- A left-side tree view of processes, top-level windows, and child controls.
- A right-side tab control with **Properties** and **Events** pages.
- A menu bar with **File**, **View**, and **Help** menus.
- A toolbar for filtering events, refreshing windows, finding elements, applying
  property changes, starting/stopping event logging, and clearing the event log.
- A status strip for current selection and workflow messages.

![ManagedSpy main window](../../screenshots/managedspy-main-window.png)

## Refresh workflow

ManagedSpy refreshes automatically on startup and can be refreshed manually with:

- **View** > **Refresh**
- The refresh toolbar button

Refresh clears the tree, enumerates top-level windows, filters them by managed
compatibility unless **Show Native Windows** is enabled, groups them by owning
process, and inserts each matching top-level window as a child tree item.

When no compatible managed processes are found, the tree displays:

- `No managed processes running.`
- `Select View->Refresh.`

## Process tree workflow

Process nodes are keyed by process ID and display:

```text
<process name>  <main window title> [<process id>]
```

Control nodes display:

```text
<component name>     [<class name>]
```

Child control nodes are populated lazily. When a node expands, ManagedSpy clears
the placeholder children and requests child proxies for each direct child window
or managed child control.

![ManagedSpy inspecting the bundled test target](../../screenshots/managedspy-inspecting-target.png)

## Property inspection

Selecting a tree node assigns that node's tag to the property grid. For managed
controls, the tag is usually a `ControlProxy` populated from target-side Windows
Forms metadata. For native fallback windows, the proxy contains less information.

The property grid can show:

- ManagedSpy metadata such as handle, process ID, and managed status.
- Type descriptor properties from the target control.
- Native fallback properties for non-managed windows.

## Applying property changes

Property changes can be applied with:

- **View** > **Apply Property**
- The **Apply** toolbar button
- `Ctrl+Enter`

The selected root property grid item is applied through the target-side
`PropertyDescriptor`. ManagedSpy blocks obvious invalid operations:

- If no control proxy is selected, nothing is applied.
- If no property is selected, the status bar asks the user to select a property.
- If the property is read-only, the status bar reports that it is read-only.
- Conversion or validation failures are displayed as warning dialogs.

Property editing is best suited for simple editable Windows Forms properties.
Complex object graphs, custom converters, read-only properties, and properties
with target-side side effects may not behave predictably.

## Event logging

Event logging is controlled by the start/stop toolbar button. When enabled,
ManagedSpy subscribes to selected events for the currently selected control. The
event filter dialog controls which events are displayed.

The event grid displays:

| Column          | Meaning                                          |
| :---            | :---                                             |
| Event Name      | The target-side event descriptor name.           |
| Event Arguments | The serialized or fallback event argument value. |

Selecting another tree node stops logging for the previous proxy, clears the
event grid, and starts logging for the new selection if the start/stop button is
still enabled.

## Event filtering

Event filtering is available through:

- **View** > **Filter Events**
- The event-filter toolbar button

The filter dialog lets the user choose which events should be subscribed and
shown. Filter changes stop and restart logging for the current selection.

## Element finder

Element finder is available through:

- **View** > **Find Element**
- The search toolbar button

When enabled:

1. The cursor changes to a crosshair.
2. A timer tracks the window under the cursor.
3. ManagedSpy highlights the currently hovered window.
4. Pressing `Esc` cancels element finder.
5. Clicking a target window stops element finder and tries to focus that window
   in the inspection tree.

If the target window is not currently present in the tree, ManagedSpy refreshes
and searches again.

## Highlighting

ManagedSpy supports temporary and persistent highlighting:

- **Show Window** flashes the selected target window several times.
- **Keep Highlighted** keeps a selected target highlighted while its bounds
  change.

Persistent highlight recalculates target bounds on a timer. It prefers
target-side Windows Forms screen bounds and falls back to raw window bounds for
DPI or accessibility mismatches. Diagnostic lines may be written to
`ManagedSpy-highlight-diagnostics.log` beside the executable when persistent
highlight decisions change.

## Show native windows

By default, ManagedSpy shows compatible managed windows only. Enabling
**View** > **Show Native Windows** and refreshing includes native fallback
windows in the tree.

Native fallback is useful for discovering handles and class names, but it does
not provide the same property, event, or child-control fidelity as full managed
inspection.

