# ADR 008 Outbox/inbox later (design)

**Status:** Proposed (not deployed)

## Context

No external event consumers are implemented yet; payment execution ownership must stay clear.

## Decision

Define versioned envelope and single payment owner before a broker.

## Alternatives and trade-offs

Service Bus fits Azure; Kafka for streaming, RabbitMQ for self-managed routing; direct HTTP simpler but publication isn't atomic.

## Consequences and revisit

Real consumers and asynchronous integration requirements. Preserve compatible persisted instructions and execution identities during change. An architectural rollback cannot reverse a bank transfer or remove audit evidence.

## Proposed envelope (documentation only)

No event publisher or consumer is implemented, so there is no unused runtime IntegrationEnvelope class. The proposed v1 envelope contains eventId, eventType, schemaVersion, tenantId, aggregateId, aggregateVersion, correlationId, causationId, occurredAt (UTC) and data. Introduce a compiled contract alongside the first real outbox publisher/consumer and compatibility tests. Payment execution retains one owner; event delivery does not authorize another bank submission.
