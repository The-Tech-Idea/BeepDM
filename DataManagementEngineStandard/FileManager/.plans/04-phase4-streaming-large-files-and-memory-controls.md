# Phase 4 - Streaming Large Files and Memory Controls

## Objective
Enable reliable ingestion of very large files with bounded memory and backpressure controls.

## Scope
- Streaming read pipeline and chunked processing.
- Memory/cpu guardrails.
- Cancellation and resumable checkpoints.

## File Targets
- `FileManager/CSVDataSource.cs`
- `FileManager/ICSVDataReader.cs`
- `FileManager/TextFieldParser.cs`

## Planned Enhancements
- Keep `CSVDataReader` as primary streaming primitive and extend with cancellation-aware iteration.
- Replace full-file rewrite patterns with bounded temp-file streaming mutations.
- Add batch/page state checkpoints for resumable long-running reads.

## Audited Hotspots
- `CSVDataSource.BulkInsert(...)`
- `CSVDataSource.InsertEntity(...)`
- `CSVDataSource.GetEntity(..., pageNumber, pageSize)`
- `CSVDataSource.GetDataReader(...)`

## Real Constraints to Address
- Insert/bulk insert currently materialize entire file in-memory (`List<string>` then `WriteAllLines`).
- Paging path computes totals while scanning full file each request (no reuse/checkpoint).
- No cancellation token or cooperative stop mechanism in long scans.

## Acceptance Criteria
- Large-file ingestion remains within memory budgets.
- Long-running reads can be cancelled safely.
- Resume flow can continue from known safe checkpoints.
