---
name: helper-skill-template
description: Template for writing BeepDM datasource helper skills with consistent sections for purpose, API surface, patterns, pitfalls, and integration examples.
---

# Helper Skill Template

Use this template when creating a new helper-focused skill in BeepDM.

## Required Frontmatter
```yaml
---
name: your-skill-name
description: Specific WHAT and WHEN guidance for the helper.
---
```

## Standard Skill Structure
1. `# Title` - helper scope in plain language.
2. `## Use this skill when` - trigger scenarios.
3. `## Responsibilities` - what the helper owns and what it does not own.
4. `## Core API Surface` - key methods grouped by operation type.
5. `## Typical Usage Pattern` - short sequence from setup to execution.
6. `## Validation and Safety` - validation checks and injection-safe patterns.
7. `## Pitfalls` - known misuse patterns and anti-patterns.
8. `## Integration Points` - links to upstream/downstream helpers and skills.
9. `## Example` - one concise code example that compiles with project conventions.

## Authoring Rules
- Keep `SKILL.md` concise and task-focused.
- Prefer real method names from source files.
- Show success/error handling via return tuples or `IErrorsInfo`.
- Explicitly mention `DataSourceType` and dialect impacts when relevant.
- Include at least one cross-link to related skills.

## Boilerplate Sections
```markdown
## Use this skill when
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

## Integration Points
- [other-skill](../other-skill/SKILL.md)
```
