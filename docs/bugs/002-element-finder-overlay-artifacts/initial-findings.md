# Initial Findings

## Confirmed Facts
- Current finder highlight uses `ControlPaint.DrawReversibleFrame` repeatedly.
- Reversible frame rendering is XOR-based and can leave stale artifacts when the underlying desktop composition changes.
- Cursor and window rectangle paths are mixed between managed APIs (`Cursor.Position`) and Win32 APIs (`GetWindowRect`), which can drift in multi-monitor/high-DPI setups.

## Likely Cause
- The XOR reversible-frame approach is fragile for modern composited desktop rendering and contributes to artifact persistence.
- Coordinate-space mismatch between cursor acquisition and highlight rectangle placement contributes to wrong-screen highlighting.

## Unknowns
- Exact DPI/monitor configurations where offset is strongest.
- Whether any additional per-monitor scaling edge cases remain after moving to a dedicated overlay.

## Reproduction Status
- User-reported reproducible issue in real usage.
- Code inspection aligns with expected failure mode.

## Evidence Gathered
- Current implementation in `MainForm.cs` uses `ControlPaint.DrawReversibleFrame` in both finder update and flashing.
- User report describes classic reversible-frame artifact behavior.
