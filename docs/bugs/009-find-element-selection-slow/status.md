# Bug Status

## Current State
awaiting-user-confirmation

## Active Attempt
`fix-attempt-005.md`

## Last Updated
2026-05-29

## Confirmation Date

## Resolution Summary
- Attempt 001 selects the finder target by directly materializing the clicked control's tree path and suppressing broad child preloading during programmatic path expansion.
- User reported attempt 001 was faster but crashed in `treeWindow_BeforeExpand`.
- Attempt 002 hardens `treeWindow_BeforeExpand` against null expansion state and child-collection mutation during expansion.
- User reported attempt 002 still crashed, now in `treeWindow_AfterSelect`.
- Attempt 003 hardens selection handling when `treeWindow.SelectedNode` is temporarily null.
- User reported attempt 003 left tree results incomplete with missing sub-components.
- Attempt 004 switches tree expansion to targeted lazy loading with placeholders and populates finder path ancestors.
- User reported the tree still does not find/show correct elements after the checkpoint commits.

## Attempt History
- `fix-attempt-001.md` - in progress for direct path selection and suppressed programmatic expansion population.
- `fix-attempt-001.md` - awaiting user confirmation after build and x86/x64 artifact tests passed.
- `fix-attempt-001.md` - user reported faster selection but a `NullReferenceException` crash during tree expansion.
- `fix-attempt-002.md` - in progress to harden tree expansion after the faster finder selection.
- `fix-attempt-002.md` - awaiting user confirmation after build and x86/x64 artifact tests passed.
- `fix-attempt-002.md` - user reported a follow-up `NullReferenceException` in `treeWindow_AfterSelect`.
- `fix-attempt-003.md` - in progress to harden selection handling for null selected-node state.
- `fix-attempt-003.md` - awaiting user confirmation after build and x86/x64 artifact tests passed.
- `fix-attempt-003.md` - user reported incomplete tree results/missing sub-components after finder selection.
- `fix-attempt-004.md` - in progress to restore complete lazy tree population while keeping the finder path fast.
- `fix-attempt-004.md` - awaiting user confirmation after build and x86/x64 artifact tests passed.
- `fix-attempt-004.md` - user reported the tree still does not find/show correct elements.
- `fix-attempt-005.md` - in progress to add lazy placeholders to refresh-created top-level nodes.
- `fix-attempt-005.md` - awaiting user confirmation after build and x86/x64 artifact tests passed.

## State Change Log
- 2026-05-29: bug opened from user report that **Find element on screen** takes a long time to select the correct tree item.
- 2026-05-29: investigation found finder selection can trigger broad tree population even though the clicked target path is already known.
- 2026-05-29: attempt 001 started.
- 2026-05-29: attempt 001 implementation, build, and x86/x64 artifact tests passed; awaiting user confirmation.
- 2026-05-29: user reported attempt 001 was faster but crashed with a `NullReferenceException` in `treeWindow_BeforeExpand`.
- 2026-05-29: attempt 002 started.
- 2026-05-29: attempt 002 implementation, build, and x86/x64 artifact tests passed; awaiting user confirmation.
- 2026-05-29: user reported attempt 002 still crashed with a `NullReferenceException` in `treeWindow_AfterSelect`.
- 2026-05-29: attempt 003 started.
- 2026-05-29: attempt 003 implementation, build, and x86/x64 artifact tests passed; copied updated `ManagedSpy.dll`/PDB into `artifacts\release`; awaiting user confirmation.
- 2026-05-29: user reported tree results are incomplete and some sub-components are missing after attempt 003.
- 2026-05-29: attempt 004 started.
- 2026-05-29: attempt 004 implementation, build, and x86/x64 artifact tests passed; copied updated `ManagedSpy.dll`/PDB into `artifacts\release`; awaiting user confirmation.
- 2026-05-29: user reported **Find element on screen** still does not find the element in the tree.
- 2026-05-29: attempt 005 started.
- 2026-05-29: attempt 005 implementation, build, and x86/x64 artifact tests passed; copied updated `ManagedSpy.dll`/PDB into `artifacts\release`; awaiting user confirmation.

## Notes
- Keep this bug open until the user confirms finder selection is fast again in their environment.
