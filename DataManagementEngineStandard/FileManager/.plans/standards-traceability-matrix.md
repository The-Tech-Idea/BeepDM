# Standards Traceability Matrix

| Standard Area | Plan Artifact | Verification Method |
|---|---|---|
| Contract clarity | Phase 1 | `CSVDataReader.GetName/GetOrdinal/GetValue` projection invariants and compatibility tests |
| Parsing correctness | Phase 2 | `CsvTextFieldParser` golden vectors + `ValidateCSVHeaders` quoted-delimiter regression tests |
| Schema/type quality | Phase 3 | `GetFieldsbyTableScan`, `AnalyzeCSVFile`, `CSVTypeMapper.ConvertValue` deterministic inference tests |
| Scalability | Phase 4 | `Insert/BulkInsert/GetEntity(page)` memory profile and cancellation/restart tests |
| Data quality | Phase 5 | `ValidateRow` enforcement tests + structured diagnostics contract tests |
| Performance | Phase 6 | Header-map cache + filter correctness microbenchmarks on `GetEntity` |
| Security/governance | Phase 7 | Path policy tests + masking assertions on datasource diagnostics |
| Extensibility | Phase 8 | Parser/datasource responsibility split tests + adapter contract compatibility suite |
| Cross-module integration | Phase 9 | Normalized row-envelope tests for ETL/Mapping/Rules handoff |
| Operations/rollout | Phase 10 | KPI gates for parse reject rate, throughput, and rollback rehearsal tests |
