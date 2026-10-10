---
name: propose-adr
description: Propose a new or changed architecture decision record (ADR) for SupportFlow. Use when a decision is not covered by the documentation, when a change would deviate from an architecture rule or an existing ADR, when an ADR status should change, or when the user asks to write, update or supersede an ADR.
---

# Propose an ADR

ADRs are agreed with the user before they are written. Never create, edit or supersede an ADR file
before the user agrees.

## 1. Gather context

- Read `docs/desicions/README.md` (process, statuses, template, registry) and every related ADR.
- Find the requirements that constrain the decision (`docs/requiremenets.md`, by section number) and the
  affected parts of `docs/domain-model.md` and `docs/architecture/`.
- Check whether the decision conflicts with an existing ADR. A changed `Accepted` ADR is not edited: a new
  ADR supersedes it.

## 2. Draft the ADR in the conversation

Write in Russian, in the style of the existing ADRs, using the template:

```markdown
# ADR-XXXX: <Название>

- **Status:** Proposed
- **Date:** <today>
- **Stage:** <stage>
- **Related:** requirements §…, ADR-…

## Problem
## Constraints
## Options
## Decision
## Trade-offs
```

- At least two real options, including the simplest one.
- Constraints cite requirement sections; do not invent constraints.
- Trade-offs list honest downsides, not only benefits.
- Mark anything not backed by the documentation as **[Assumption]**.

Show the draft and list the documents that would change with it. Ask the user to agree, adjust or reject.

## 3. After agreement

1. Create `docs/desicions/NNNN-<kebab-case-title>.md` with the next free number.
2. Add the row to the registry in `docs/desicions/README.md`.
3. If it supersedes an ADR, set the old one's status to `Superseded by ADR-NNNN`.
4. Update the documents that describe the decision (`domain-model.md`, `docs/architecture/*.md`) so they
   do not contradict it.
5. Report the changed files.
