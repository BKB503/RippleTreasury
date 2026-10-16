# Architecture decision records

Each record explains one decision, alternatives and consequences. ADR 004 additionally provides the detailed implementation walkthrough. Status distinguishes local implementation from production proposals.

| ADR | Decision | Status |
|---|---|---|
| 001 | [Clean Architecture and feature folders](001-modular-monolith.md) | Accepted (local implementation) |
| 002 | [DDD financial rules](002-domain-rules.md) | Accepted (local implementation) |
| 003 | [SQLite persistence and initialization](003-sqlite-storage.md) | Accepted (local implementation) |
| 004 | [Batch request and background-worker lifecycle](004-durable-processing.md) | Accepted (local implementation) |
| 005 | [Idempotency over exactly-once claims](005-idempotency.md) | Accepted (local implementation) |
| 006 | [API models and independent versions](006-versioning.md) | Accepted (local implementation) |
| 007 | [Audit plus OTel](007-observability.md) | Accepted (local implementation) |
| 008 | [Outbox/inbox later (design)](008-outbox.md) | Proposed (not deployed) |
| 009 | [Container Apps + Bicep (design)](009-azure-hosting.md) | Proposed (not deployed) |
| 010 | [Lightweight UI](010-lightweight-ui.md) | Accepted (local implementation) |
| 012 | [Purpose-specific immutable party snapshots](012-party-snapshots.md) | Accepted |
