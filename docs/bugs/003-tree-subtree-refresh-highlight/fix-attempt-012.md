# Fix Attempt 012

## Attempt Status
implemented-awaiting-user-confirmation

## Goal
Make `Keep Highlighted` follow the rendered bounds of custom child controls whose visible region differs from their raw WinForms/HWND geometry.

## Relation To Previous Attempts
Follow-up to `fix-attempt-011.md`, which moved child-control geometry onto native HWND coordinates but did not change the user-visible behavior. That suggests the selected custom control's visible region is not adequately described by either the WinForms screen-coordinate path or the raw HWND client rectangle alone.

## Proposed Change
- Prefer the selected control's accessibility bounds for child-control highlighting.
- Keep the native HWND client-rectangle and WinForms coordinate paths as fallbacks.
- Add regression coverage proving the helper honors a control-specific accessibility rectangle when available.

## Risks
- Some custom controls may expose incomplete accessibility data, so the native HWND path still needs to remain available.
- Accessibility bounds can represent the semantic element rather than the full painted region; this is intentional because it matches the user's Accessibility Insights comparison more closely.

## Files And Components
- `ManagedSpyLib\ScreenBoundsHelper.cpp`
- `ManagedSpy.Tests\ScreenBoundsHelperTests.cs`
- `docs\bugs\003-tree-subtree-refresh-highlight\status.md`

## Verification Plan
- Build solution for `Release|x86` and `Release|x64`.
- Run automated tests for the helper in x64.
- User retest on the same custom wizard-option scenario.

## Implementation Summary
- Added an accessibility-bounds probe in `ScreenBoundsHelper` and made it the first child-control geometry source.
- Preserved the native HWND coordinate mapping and prior clipping behavior as fallbacks.
- Added a WinForms test control with a custom accessibility object and verified the helper prefers its accessibility bounds.
- Suppressed the C++/CLI `C4642` import warning locally around the `AccessibleObject` usage introduced by this attempt.

## Test Results
- `.\build.ps1 -OutputRoot C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt12d` succeeded for Release x86/x64.
- `dotnet vstest C:\Users\david\.copilot\session-state\a1965201-d7d9-4e66-9c57-c0ab309d9cf4\files\build-out-attempt12d\x64\ManagedSpy.Tests.dll /Platform:x64` passed: 4/4 tests.

## Outcome
Persistent highlight now prefers the same accessibility-oriented bounds source that tools like Accessibility Insights rely on for custom controls, while still keeping the previously added native and clipped fallbacks.

## Next Step
User confirmation on the wizard-option scenario that remained offset after attempt 011.

## Remaining Gaps
- Behavioral confirmation in the user's application is still pending.
