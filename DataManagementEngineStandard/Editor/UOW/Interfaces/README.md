# UnitOfWork Interfaces

## Purpose
This folder defines helper contracts used by unit-of-work orchestration. The interfaces isolate defaults, validation, state management, eventing, and collection behavior from the main unit-of-work implementation.

## Key Interfaces
- `IUnitofWorkDefaults`: Applies insert/update defaults through resolver context.
- `IUnitofWorkValidation`: Entity, key, and required-field validation.
- `IUnitofWorkDataHelper`: Entity cloning, conversion, and value extraction.
- `IUnitofWorkStateHelper`: Entity state tracking (`Added`, `Modified`, `Deleted`, etc.).
- `IUnitofWorkEventHelper`: Event payload construction and pre/post event dispatch.
- `IUnitofWorkCollectionHelper`: Filtering, paging, sorting, and collection synchronization.
- `DefaultValueContext`: Context model for resolver-driven default application.

## Integration Notes
- Keep these contracts independent from UI concerns.
- Favor pure helper behavior so unit tests can cover each concern in isolation.
- Ensure compatibility with `ObservableBindingList<T>` and editor event pipelines.
