# Bug Description

## Title
Managed applications are not detected after the ported build is launched from normal project output

## Status
- fixed

## Reported Symptoms
- The ported application only sees native windows.
- Running two ManagedSpy instances does not let one detect the other as a managed application.
- Managed inspection behavior no longer matches the original application.
- After the hook deployment fix, starting another ManagedSpy instance causes the inspected instance to freeze.

## Expected Behavior
- A normal build and launch of the ported application should still detect managed WinForms applications, including another ManagedSpy instance of the same bitness.

## Actual Behavior
- The app falls back to native-window behavior because the cross-process hook cannot be installed.
- Managed windows are enumerated, but they are never upgraded to managed `ControlProxy` instances.
- In the latest retest, the failure mode escalated to an application freeze when another ManagedSpy instance starts.

## Reproduction Details
1. Build `ManagedSpy\ManagedSpy.csproj`.
2. Launch the built application from `ManagedSpy\bin\<platform>\<configuration>\`.
3. Start another compatible managed WinForms app, such as another ManagedSpy instance.
4. Refresh the tree.
5. Only native windows appear.

## Affected Area
- `ManagedSpy\ManagedSpy.csproj`
- Hook shim deployment (`ManagedSpyHook.dll`)
- `ManagedSpyLib\Desktop.cs`

## Constraints
- Preserve the managed-port structure.
- Keep the hook shim as the remaining native boundary.
- Make the normal project build path behave like the existing solution artifact build path.

## Open Questions
- None currently. Source inspection already identifies the missing deployment path.
