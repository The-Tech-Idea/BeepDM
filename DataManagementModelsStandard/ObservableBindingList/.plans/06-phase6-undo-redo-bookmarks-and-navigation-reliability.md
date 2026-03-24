# Phase 6 - Undo/Redo, Bookmarks, and Navigation Reliability

## Objective
Harden history and navigation semantics for long interactive sessions.

## Audited Hotspots
- `ObservableBindingList.UndoRedo.cs`: `RecordPropertyChangeForUndo`, `Undo`, `Redo`, action replay.
- `UndoRedoManager.cs`: history stack bounds and clear semantics.
- `ObservableBindingList.CurrentAndMovement.cs`: movement events and BOF/EOF.
- `ObservableBindingList.Bookmarks.cs`: bookmark validity after mutations.

## File Targets
- `ObservableBindingList.UndoRedo.cs`
- `UndoRedoManager.cs`
- `ObservableBindingList.Bookmarks.cs`
- `ObservableBindingList.CurrentAndMovement.cs`

## Real Constraints to Address
- Replay ordering must stay correct when list shape changes between record and replay.
- Bookmark/current selection must survive remove/filter/page transitions.
- Undo history growth needs strict bound and trimming strategy.

## Acceptance Criteria
- Undo/redo history is bounded and consistent.
- Bookmark/current-item behavior survives list mutations.
- Navigation commands are deterministic and exception-safe.
