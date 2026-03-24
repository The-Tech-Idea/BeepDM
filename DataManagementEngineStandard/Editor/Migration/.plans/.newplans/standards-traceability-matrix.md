# Standards Traceability Matrix (Focused Scope)

| Standard / Expectation | Target Hotspot(s) | Verification Method |
|---|---|---|
| Explicit and discovery migration paths are behaviorally equivalent for same entities | Hotspots 1-2 | parity tests comparing summary payloads and decision codes |
| Readiness and summary outputs are machine-gatable | Hotspots 3-4 | CI contract validation using structured report schema |
| Entity-level DDL operations expose deterministic outcomes | Hotspots 5-6 | operation outcome classification tests per DDL method |
| Discovery is reproducible and diagnosable | Hotspots 7-8 | deterministic ordering tests + discovery evidence snapshots |
| Datasource best-practice guidance is capability-backed and versioned | Hotspots 9-10 | recommendation profile tests and probe-to-recommendation mapping checks |

## Completion Definition
- All focused hotspots have coded verification artifacts.
- No critical migration path relies on free-text parsing for policy gates.
- Discovery and DDL operation evidence is available for post-run audit.
