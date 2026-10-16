# ADR 010 Lightweight operations UI

**Status:** Accepted (local implementation)

## Context

Reviewers need to generate representative batches, inspect progress and resolve uncertain outcomes without an enterprise portal.

## Decision

Use React/TypeScript/MUI, React Router, TanStack Query for server data and local useState. The current user-requested surfaces are Dashboard & Reports, Process Batch, Batches and Reconciliation. Process Batch is the explicitly requested addition to the original three-surface plan. Use no Redux or Zustand.

The UI calls versioned HTTP APIs and displays their responses. Domain/Application owns money validation, progress decisions and retry eligibility. Application response mappers format exact minor-unit strings and currency-separated totals; JSON remains stable through the CLR contract renaming.

## Alternatives and trade-offs

Global stores and a full enterprise portal add unnecessary complexity. Bounded in-memory reporting is sufficient for this demo but requires database projections/paging at production scale. Bundled hosting simplifies local startup; separate Azure Static Web Apps remains a design option.

## Consequences and revisit

Keep reconciliation actions evidence-based; a recheck cannot blindly submit another transfer. Add permission-aware workflows, evidence upload or live updates only with a concrete requirement. Revisit reporting storage when measured volume demands it.
