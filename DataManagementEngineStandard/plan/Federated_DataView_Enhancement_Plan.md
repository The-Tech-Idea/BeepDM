# DataView Federation Enhancement Plan

## 1. Executive Summary
Currently, `DataViewDataSource` in BeepDM implements cross-datasource queries by eagerly loading entire entity datasets from source databases into a temporary `InMemoryDB` or `LocalDB` (via `PrepareMergedQueryDB()`), and then executing the query locally. While this approach works for small datasets, it functions more as an implicit ETL script rather than a true federated query engine. It does not scale to large datasets and suffers from high latency and memory overhead.

This enhancement plan outlines the transformation of `DataViewDataSource` into a robust Data Federation / Data Virtualization engine, drawing architectural inspiration from well-known enterprise tools like **Trino/Presto**, **Dremio**, and **Denodo**.

## 2. Architectural Pillars of Enterprise Federation

To reach enterprise parity, the federated engine must adhere to the following core principles:
1. **Predicate Pushdown & Query Delegation**: Push filtering (`WHERE`), column selection (`SELECT`), and aggregations (`GROUP BY`) down to the source database whenever possible to minimize data transfer.
2. **Streaming Data Transfer**: Avoid materializing entire tables in memory. Data should be streamed or bulk-loaded efficiently.
3. **Smart Caching & Materializations (Data Reflections)**: Allow administrators to choose between "Direct Query" (federated live) and "Import" (cached/materialized) execution modes.
4. **Native Query Execution**: Leverage modern OLAP engines (like DuckDB) natively instead of copying data row-by-row in C#.

---

## 3. Phased Enhancement Plan

### Phase 1: Query Parser & Predicate Pushdown (The Query Planner)
**Goal:** Stop pulling `SELECT *`. Pull only what is needed.
- **Action:** Integrate a lightweight SQL parser (or enhance the existing `RunQuery` logic) to parse cross-datasource SQL queries into an Abstract Syntax Tree (AST).
- **Action:** Before transferring data to the temporary DB, extract the query filters for each specific entity.
- **Action:** Translate these AST filters into BeepDM's `AppFilter` objects.
- **Action:** Pass these filters into `sourceDs.GetEntity(EntityName, appFilters)` to ensure the source database performs the filtering before sending data over the network.

### Phase 2: Generalized In-Memory & Local Execution Engine
**Goal:** Replace C#-level row-by-row iteration (`UpdateEntities`) with optimized engine-level operations leveraging any available local/in-memory database.
- **Context:** The federation engine should dynamically discover and utilize any available data source that implements `ILocalDB` or `IInMemoryDB`. The target list can be dynamically discovered via: `DMEEditor.ConfigEditor.DataConnections.Where(p => p.Category == DatasourceCategory.INMEMORY || p.IsLocal).ToList()`.
- **Action:** Once a target local engine is selected from the list, materialize entities into it.
- **Action:** If the discovered execution engine happens to be DuckDB, take advantage of its native scanners (Postgres, MySQL, SQLite, Parquet, CSV) by generating `ATTACH` or `CREATE VIEW` statements, bypassing the C# layer entirely for supported sources.
- **Action:** For other local engines (e.g., SQLite or other generic local DBs), use them as highly efficient, lightweight transactional fallbacks for UI caching or intermediate dataset joins via optimized bulk operations.

### Phase 3: Fast ETL Fallback for Unsupported Sources
**Goal:** Handle NoSQL, APIs, or legacy databases without native DuckDB scanners efficiently.
- **Action:** If an entity must be moved to the temporary DB because native federation isn't supported, utilize BeepDM's `etl` skills (e.g., `ETLEditor.RunCreateScript()`).
- **Action:** Instead of loading generic `IEnumerable<object>` into memory, stream the results directly. Use DuckDB's Appender API or write the stream to a temporary `.parquet` file locally, which the local engine can query instantly, completely bypassing the GC memory overhead.

### Phase 4: Caching & "Data Reflections" (Materialized Views)
**Goal:** Improve performance for slow source systems and heavy analytical queries (similar to Dremio's Data Reflections).
- **Action:** Extend the `EntityStructure` within `DataView` to support a `ExecutionMode` property (`DirectQuery` vs `Cached`).
- **Action:** Add caching metadata such as `CacheTTL` (Time To Live).
- **Action:** When `PrepareMergedQueryDB()` runs, check if the cache is still valid. If it is, use the existing local data. If it has expired, trigger an asynchronous ETL synchronization using `BeepSync` or `ETLScriptManager` to incrementally update the local materialized view in the background.

### Phase 5: Pagination and Streaming API
**Goal:** Ensure enterprise-grade stability under heavy data loads.
- **Action:** Modify `RunQuery` to return an `IAsyncEnumerable<object>` or leverage `PagedResult` to stream query results back to the caller (e.g., BeepService Web API or Blazor Grid).
- **Action:** Prevents Out-Of-Memory (OOM) exceptions when a federated join results in millions of rows.

---

## 4. Work Breakdown & Implementation Details

| Component | Target File | Specific Tasks |
| :--- | :--- | :--- |
| **DataViewDataSource** | `DataViewDataSource.cs` | Refactor `PrepareMergedQueryDB()`. Add query parsing layer. Replace `sourceDB.GetEntity(..., new List<AppFilter>())` with dynamic, parsed `AppFilter`s. |
| **Execution Engine** | `DataViewDataSource.cs` | Add logic to detect if `targetDB` is `DuckDB`. If so, inject native `ATTACH database` commands for supported SQL sources. |
| **Entity metadata** | `EntityStructure.cs` | Add `ExecutionMode`, `TTL`, `LastRefresh` properties to support materialized caching decisions. |
| **File I/O** | `Parquet/ETL` | Add internal helper to export `IEnumerable<object>` to temporary `.parquet` for faster bulk loading into `IInMemoryDB`. |

## 5. Summary
By shifting from the current **"ETL-to-Memory"** approach to a **"Pushdown-and-Native-Scan"** architecture, `DataViewDataSource` will become a true federated query engine capable of safely joining tables with millions of rows across disconnected SQL and NoSQL databases.
