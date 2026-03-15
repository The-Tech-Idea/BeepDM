---
name: helper-skill-template
description: Template for writing BeepDM datasource helper skills with consistent sections for purpose, API surface, patterns, pitfalls, and integration examples.
---

# Helper Skill Template

Use this template when creating or upgrading a helper-focused BeepDM skill. Keep `SKILL.md` as the routing layer and move longer method inventories into `reference.md`.

## Required Frontmatter
```yaml
---
name: your-skill-name
description: Specific WHAT and WHEN guidance for the helper.
---
```

## Required Files
- `SKILL.md` for triggers, scope, workflow, pitfalls, and one small example
- `reference.md` for method lists, extended code snippets, troubleshooting, and option matrices

## Standard Skill Structure
1. `# Title` - helper scope in plain language.
2. `## Use this skill when` - trigger scenarios.
3. `## Do not use this skill when` - redirect to adjacent skills to avoid overlap.
4. `## Responsibilities` - what the helper owns and what it does not own.
5. `## Core API Surface` - only the highest-signal methods, grouped by operation type.
6. `## Typical Usage Pattern` - short sequence from setup to execution.
7. `## Validation and Safety` - validation checks and injection-safe patterns.
8. `## Pitfalls` - known misuse patterns and anti-patterns.
9. `## File Locations` - source files that ground the skill.
10. `## Related Skills` - explicit cross-links.
11. `## Example` - one concise example that matches project conventions.
12. `## Detailed Reference` - link to `reference.md`.

## Authoring Rules
- Keep `SKILL.md` concise and task-focused.
- Prefer real method names from source files.
- Show success/error handling via return tuples or `IErrorsInfo`.
- Explicitly mention `DataSourceType` and dialect impacts when relevant.
- Include at least one cross-link to related skills.
- Verify file paths exist in the current repo before publishing them in the skill.
- When upgrading an existing skill, update `SKILL.md` and `reference.md` together so they do not drift.
- Split into a new skill only when the API surface or workflow is large enough to deserve separate routing.

## Quality Bar
- The description explains both the responsibility and the trigger.
- The skill can answer "when should I use this instead of another skill?"
- The workflow names the actual BeepDM types used in the codebase.
- The example is short enough to scan and realistic enough to reuse.
- The related-skill links form a usable path to the next narrower skill.

## Enhancement Checklist
1. Read the current `SKILL.md` and `reference.md`.
2. Verify the main API surface with `rg` against the repository.
3. Remove duplicated detail from `SKILL.md`; move it to `reference.md`.
4. Add or tighten adjacent-skill links so routing is explicit.
5. Recheck the README entry if the skill's scope changed.

## Boilerplate Sections
```markdown
## Use this skill when
- ...

## Do not use this skill when
- ...

## Responsibilities
- ...

## Core API Surface
- Schema:
  - ...
- DDL:
  - ...
- DML:
  - ...

## Typical Usage Pattern
1. ...
2. ...
3. ...

## Validation and Safety
- ...

## Pitfalls
- ...

## File Locations
- path/to/source.cs

## Related Skills
- [other-skill](../other-skill/SKILL.md)

## Detailed Reference
Use [reference.md](./reference.md) for ...
```
