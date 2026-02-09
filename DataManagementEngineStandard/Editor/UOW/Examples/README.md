# UnitOfWork Examples

## Purpose
This folder provides runnable usage examples for unit-of-work patterns, including tracking changes, applying defaults, and committing entity operations.

## Key Files
- `UnitofWorkExamples.cs`: End-to-end examples using sample entities (`Customer`) and unit-of-work operations.

## How To Use
1. Initialize a unit-of-work with entity metadata.
2. Perform add/update/delete operations and inspect tracked state.
3. Execute commit flows and observe event and validation behavior.

## Extension Guidelines
- Keep samples aligned with current interface contracts.
- Promote stable examples to automated tests for regression coverage.
