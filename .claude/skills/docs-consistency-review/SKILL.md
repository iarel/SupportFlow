---
name: docs-consistency-review
description: Check SupportFlow documentation for consistency — requirements, domain model, C4 architecture documents, architecture.md and ADRs against each other, against CLAUDE.md and against the code. Use when asked to check, audit or analyze the docs or architecture, after documentation changes, or before accepting ADRs.
---

# Documentation consistency review

Read-only: report findings, do not change files unless the user asks afterwards.

## 1. Read the sources

- `docs/requiremenets.md`
- `docs/domain-model.md`
- `docs/architecture/context.md`, `containers.md`, `components.md`, `architecture.md`
- every ADR in `docs/desicions/` and its registry in `README.md`
- `CLAUDE.md`
- the code structure under `src/` and `tests/` when the documents describe it

## 2. Check

- **Requirements coverage** — every functional requirement and NFR section is addressed or explicitly out
  of scope; nothing contradicts a requirement.
- **Cross-document agreement** — the same concept (statuses, fields, events, error codes, mechanisms, module
  list, project names) is described the same way everywhere. Diagrams match the text and the ADRs.
- **ADR integrity** — registry matches the files; statuses are consistent; no two ADRs decide the same
  question differently; superseded ADRs are marked.
- **Assumptions** — `[Assumption]` items and open questions are listed, and no document treats them as
  decided.
- **Documents vs code** — project names, namespaces, folders and registrations match what the documents
  say; documents do not describe components as existing when they are not.
- **Broken references** — links to files, sections and ADR numbers resolve.

## 3. Report

Group findings by severity: contradictions, gaps (requirement not covered), stale descriptions, minor
issues. For each: the files and lines involved, what disagrees, and a proposed resolution. Changes to ADRs
go through the `propose-adr` skill.
