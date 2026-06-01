# DataBase Utilities

## Overview

Shared database-level support components used across BeepDM data operations.

## Key Files

- `ObservableBindingListConverter.cs` - Converter utilities for observable/binding list interoperability

## Features

- Binding list conversion utilities
- Observable collection adapters
- UI-binding scenario support
- Data operation helpers

## How It Fits

Provides small reusable infrastructure used by:
- Data operations and CRUD workflows
- UI-binding scenarios (WinForms, WPF, Blazor)
- UnitOfWork data binding
- FormsManager block binding

## Related Documentation

- [Core Architecture](../Docs/CoreArchitecture.md)
- [Unit of Work Pattern](../Docs/UnitOfWork.md)
- [Forms Manager](../Docs/FormsManager.md)
