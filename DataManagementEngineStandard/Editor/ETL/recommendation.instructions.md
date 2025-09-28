# ETL defaults application recommendation

Goal
- Ensure destination records produced by ETL always receive proper default values (static and rule-based) configured via `DefaultsManager`, without overwriting explicitly mapped data.

Rationale
- Current ETL transformation assigns defaults unconditionally in some paths and duplicates logic already available in `MappingDefaultsHelper`.
- Centralizing default application reduces bugs, honors future resolver enhancements, and keeps behavior consistent with Mapping Manager.

Recommendations
1) Apply defaults after mapping and before insert:
   - For each transformed destination object, call `MappingDefaultsHelper.ApplyDefaultsToObject` with:
     - `destDataSourceName`
     - destination entity name
     - the transformed object
     - destination entity fields metadata
   - This helper uses `DefaultsManager` and only sets a default when the current value is null/default.

2) Replace direct, per-field default loops in ETL code with the helper:
   - Avoid duplicating resolution logic (rules, static values, parameter passing).

3) Preserve existing values:
   - Do not override values explicitly mapped from source.
   - The helper already guards against this.

4) Parameter context for rules:
   - Use `PassedArgs` with entity and datasource names, as done in the helper.

Implementation plan
- Update `ETLDataCopier.TransformData` to:
  - Remove manual default application loop.
  - Call `MappingDefaultsHelper.ApplyDefaultsToObject` per transformed record, using `DestEntityStructure` metadata available in the class.

Acceptance criteria
- Build succeeds.
- Defaults are applied only to missing/null destination fields.
- Rule and static defaults produce same values as `DefaultsManager` examples.
- No behavior change for mapped (non-null) fields.
