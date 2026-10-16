# ADR 001 Clean Architecture and feature folders

**Status:** Accepted (local implementation)

## Context

The code must be understandable to a reviewer while keeping one payment execution owner and independently runnable API/worker hosts.

## Decision

Use a modular monolith with dependencies pointing inward:

- Domain contains entities, value objects, financial rules and state transitions. It references no EF, HTTP or Azure packages.
- Application orchestrates use cases, owns repository/gateway ports and maps read results to response models. Batch, payment, reconciliation and reporting mappers live beside their features.
- Infrastructure implements ports using EF, HTTP bank adapters and durable claims. Persistence contains Repositories, Configurations, Migrations and Transactions; there is no second set of database entities.
- API controllers handle HTTP binding and invoke Application. Batch intake is exposed through IBatchService, implemented by BatchService and registered through DI. Worker invokes the same application services; neither owns financial rules.
- Contracts contains versioned request/response models, independent of persistence entities.

Keep one meaningful type per file. Use narrow interfaces and concrete dependency injection, not a generic repository, mediator framework or one project per feature. Execution, reconciliation and recovery share one repository owner with focused partial files.

## Alternatives and trade-offs

Immediate microservices add distributed consistency and deployment overhead. Separate persistence entities can suit incompatible legacy schemas, but would duplicate the current model and require conversion code. Feature folders give navigation and replacement boundaries without extra projects.

## Consequences and revisit

Application owns query contracts and response projections; Infrastructure does not expose IQueryable. Reporting currently materializes bounded demo data. Add database-side paging or split services only when measured scale or ownership boundaries justify them.
