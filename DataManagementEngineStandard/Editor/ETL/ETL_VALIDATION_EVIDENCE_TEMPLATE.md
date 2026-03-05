# ETL Validation Evidence Template

Use this file to record formal validation evidence for integrated ETL runs.

## Run Identity

- RunId:
- Date:
- Environment:
- Provider:
- Entry point: (`RunCreateScript` / `RunImportScript`)

## Scenario

- Scenario type: (create-only / copy-only / create+copy / import bridge / migration delta / save-load)
- Source datasource:
- Destination datasource:
- Target entities:

## Expected Outcome

- Expected steps:
- Expected records:
- Expected fallback behavior (if any):

## Observed Outcome

- Steps total/succeeded/failed:
- Records processed:
- Cancelled:
- Stop threshold triggered:
- Retry/fallback triggered:

## Telemetry Checks

- Correlation id present:
- Summary line present:
- `LoadDataLogs` coherent:
- Tracking entries coherent:

## Result

- Status: (PASS / FAIL)
- Notes:
- Follow-up actions:
