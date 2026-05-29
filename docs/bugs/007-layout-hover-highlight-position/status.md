# Bug Status

## Current State
fixed

## Active Attempt
`fix-attempt-002.md`

## Last Updated
2026-05-29

## Confirmation Date
2026-05-29

## Resolution Summary
- Attempt 001 now routes layout rectangles through `ScreenBoundsHelper` so Layout hover uses normalized, ancestor-clipped screen rectangles before ManagedSpy applies the existing overlay highlight pipeline.
- Attempt 002 now anchors Layout hover rectangles to the same resolved element rectangle used by persistent highlight, then maps the selected box-model section relative to that anchor.
- User confirmed the Layout highlight works after attempt 002.

## Attempt History
- `fix-attempt-001.md` - in progress for layout hover highlight coordinate normalization.
- `fix-attempt-001.md` - awaiting user confirmation after build and x86/x64 artifact tests passed.
- `fix-attempt-001.md` - user reported the Layout highlight was still broken.
- `fix-attempt-002.md` - awaiting user confirmation after anchoring layout hover to the persistent-highlight rectangle path.
- `fix-attempt-002.md` - user confirmed fixed.

## State Change Log
- 2026-05-28: bug opened from user report that Layout tab hover highlight is drawn far from the target area.
- 2026-05-28: investigation found a likely coordinate-space mismatch between `ControlLayoutInfo` rectangles and the existing overlay normalization path.
- 2026-05-28: attempt 001 started.
- 2026-05-28: attempt 001 updated `ControlLayoutInfo` to generate normalized screen rectangles through `ScreenBoundsHelper` and added layout regression coverage.
- 2026-05-28: focused review found layout rectangles also needed ancestor client clipping and an independent clipped-control test; attempt 001 was updated accordingly.
- 2026-05-28: build and x86/x64 artifact tests passed; awaiting user confirmation.
- 2026-05-29: user reported Layout highlight was still broken after attempt 001.
- 2026-05-29: attempt 002 started to reuse the persistent-highlight rectangle path as the overlay anchor.
- 2026-05-29: attempt 002 implementation, build, and x86/x64 artifact tests passed; awaiting user confirmation.
- 2026-05-29: user confirmed the Layout highlight works.

## Notes
- Confirmed fixed by user on 2026-05-29.
