# /adr — Create Architecture Decision Record

Prompt the user for:
1. Decision title (e.g., "Use MongoDB for telemetry instead of TimescaleDB")
2. Context / problem statement
3. Options considered (minimum 2)

Then generate an ADR file at `docs/adr/NNN-{slug}.md` with this structure:

```markdown
# ADR-NNN: [Title]

## Status
Proposed | Accepted | Deprecated | Superseded by ADR-XXX

## Context
[What is the issue that we're seeing that is motivating this decision or change?]

## Decision
[What is the change that we're proposing or have agreed to implement?]

## Consequences
[What becomes easier or more difficult to do because of this change?]

## 5-Perspective Analysis
- **CEO:** [Business value, cost impact, competitive advantage]
- **Architect:** [Scalability, maintainability, technical debt]
- **Dev:** [Implementation complexity, testing, developer experience]
- **PM:** [Timeline, risk, dependencies, deliverability]
- **User:** [Impact on UX, performance, intuitiveness]

## Options Considered
| Option | Pros | Cons |
|--------|------|------|
| [Option A] | ... | ... |
| [Option B] | ... | ... |
```

Use the next available ADR number. Update `docs/adr/README.md` index.
