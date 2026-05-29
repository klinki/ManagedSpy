# ManagedSpy Usage Manual

This manual explains how to build, launch, and use the current .NET 10 ManagedSpy
port.

## Build ManagedSpy

Build requirements are documented in the repository [README](../../Readme.md).
The normal release build is:

```powershell
.\build.ps1
```

The build creates:

- `artifacts\release\x64\ManagedSpy.exe`
- `artifacts\release\x86\ManagedSpy.exe`

Use `-Platform` to build only one architecture:

```powershell
.\build.ps1 -Platform x64
.\build.ps1 -Platform x86
```

## Choose the correct architecture

Choose the ManagedSpy build that matches the target process:

| Target process | ManagedSpy build to use                  |
| :---           | :---                                     |
| 64-bit target  | `artifacts\release\x64\ManagedSpy.exe`   |
| 32-bit target  | `artifacts\release\x86\ManagedSpy.exe`   |

Do not launch the x86 DLL through the default x64 `dotnet` host. Prefer the
generated executable:

```powershell
.\artifacts\release\x86\ManagedSpy.exe
```

If DLL launch is required, use the matching host:

```powershell
& "${env:ProgramFiles(x86)}\dotnet\dotnet.exe" .\artifacts\release\x86\ManagedSpy.dll
```

## Start inspecting

1. Start the target Windows Forms application.
2. Start the matching ManagedSpy executable.
3. If the target is not listed, choose **View** > **Refresh**.
4. Expand the target process node.
5. Select a window or control node.
6. Inspect the **Properties** or **Layout** tab.
7. Use **Highlighted Items** to revisit any targets that you keep highlighted.

The bundled test target can be used as a known-compatible example:

```powershell
.\artifacts\release\x64\ManagedSpy.TestTarget.exe
.\artifacts\release\x64\ManagedSpy.exe
```

![ManagedSpy test target application](../../screenshots/managedspy-test-target.png)

## Inspect properties

After selecting a tree item, inspect values in the **Properties** tab.

To edit a property:

1. Select an editable property in the property grid.
2. Change its value.
3. Choose **View** > **Apply Property**, click **Apply**, or press `Ctrl+Enter`.

If the property is read-only or cannot be converted, ManagedSpy reports the
problem in the status bar or warning dialog.

## Log events

1. Select a managed control in the tree.
2. Choose **View** > **Filter Events** to select events of interest.
3. Click the start/stop event logging toolbar button.
4. Interact with the target application.
5. Review event rows in the **Events** tab.
6. Click the same toolbar button again to stop logging.

Event logging is selection-specific. Selecting another control stops logging for
the previous control and restarts it for the new one when event logging is
enabled.

## Inspect layout

After selecting a managed control, use the **Layout** tab to inspect its WinForms
box model:

- `Margin` shows the selected control's margin values.
- `Element` shows the selected control size.
- `Padding` shows the selected control's padding values.
- `Content` shows the display/content size from the control's
  `DisplayRectangle`.

Hover a section in the Layout tab to highlight the corresponding target
rectangle on screen.

## Find an element on screen

1. Choose **View** > **Find Element** or click the search toolbar button.
2. Move the cursor over the target application.
3. Watch the temporary highlight frame.
4. Click the desired element to select it in the tree.
5. Press `Esc` to cancel without selecting.

Element finder works best when the target window belongs to a compatible managed
Windows Forms process. For native fallback windows, selection may stop at the
nearest Win32 handle.

## Highlight a selected window

Use these commands from the tree context menu:

- **Show Window** flashes the selected target.
- **Refresh Subtree** reloads the selected control subtree.
- **Keep Highlighted** toggles a persistent overlay for the selected target while
  it moves or resizes. Multiple targets can be kept highlighted at once, and each
  target receives its own color.

Active persistent highlights appear in **Highlighted Items** below the tree. Each
row shows the highlight color and target label. Click a row to select that target
in the tree and show its properties. Use **Remove All Highlights** to clear every
active persistent highlight.

Persistent highlighting writes diagnostic data to
`ManagedSpy-highlight-diagnostics.log` next to the executable when its selected
rectangle changes.

## Show native windows

Enable **View** > **Show Native Windows**, then refresh. ManagedSpy will include
native fallback windows that are normally hidden by the managed-only filter.

Use this mode when:

- You need to confirm whether a process has visible windows.
- You need a handle or Win32 class name.
- You are diagnosing why a process is not considered compatible.

Disable it again when you want to return to managed Windows Forms controls only.

## Troubleshooting

| Symptom                                           | Likely cause and action                                                                 |
| :---                                              | :---                                                                                    |
| Target is not listed                              | Verify bitness, runtime compatibility, and choose **View** > **Refresh**.               |
| x86 DLL launch fails with architecture mismatch   | Launch `ManagedSpy.exe` or use the x86 `dotnet.exe` path.                               |
| Elevated target is not visible                    | Run ManagedSpy elevated or inspect a non-elevated target.                               |
| Process appears only with **Show Native Windows** | The process is not passing the managed Windows Forms compatibility checks.              |
| Properties are sparse                             | The selected node is likely a native fallback proxy, not a managed `ControlProxy`.      |
| Property edit does not apply                      | The property may be read-only, not convertible, or rejected by target-side validation.  |
| Event logging is empty                            | Check the event filter, select a managed control, then restart event logging.           |
| Highlight bounds look wrong                       | DPI, accessibility bounds, or target custom drawing may affect calculated rectangles.   |
| ManagedSpy hangs briefly                          | The target may be busy; IPC uses timeouts, but slow targets can still delay responses.  |

## Validation commands

For full local validation, run:

```powershell
.\build.ps1
dotnet vstest .\artifacts\release\x64\ManagedSpy.Tests.dll /Platform:x64
dotnet vstest .\artifacts\release\x86\ManagedSpy.Tests.dll /Platform:x86
```

The full artifact tests include an automated UIAutomation workflow that launches
ManagedSpy and the bundled test target. The test requires an interactive Windows
desktop.
