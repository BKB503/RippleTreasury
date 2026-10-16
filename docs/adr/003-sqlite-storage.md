# ADR 003 SQLite persistence and schema initialization

**Status:** Accepted (local implementation)

## Context

Reviewers need repeatable local storage without a database server. Production requires different concurrency, availability and release controls.

## Decision

Use EF Core with SQLite locally. TreasuryDb exposes entity-aligned DbSets: PaymentBatches, PaymentInstructions, PaymentExecutions, PaymentAttempts, ReconciliationCases, PaymentAuditEvents, PaymentPartySnapshots, IdempotencyRequests, Tenants and SimulatedBankExecutions. Repositories have responsibility names without a Sqlite prefix; only this provider is implemented and tested.

Configurations explicitly retain the physical table names. SimulatedBankExecution maps to the existing MockBankExecutions table; the naming refactor changes no stored data or SQL schema.

TreasuryDatabaseInitializer.InitializeAsync applies migrations, enables WAL and installs immutable-data triggers. It does not seed payments, delete databases or recover bank outcomes. The tenant is migration reference data; sample payments are submitted through the Process batch UI and normal API.

## Alternatives and trade-offs

SQLite serializes writers and is unsuitable for the proposed multi-replica production deployment. PostgreSQL is the Azure design; SQL Server/Oracle are alternatives. Each requires provider-specific migrations, claim SQL and fault/concurrency tests, not a connection-string-only switch.

## Consequences and revisit

Production applies reviewed migrations in a controlled job before compatible hosts start. Keep database backup/restore and schema compatibility explicit. Revisit the provider when production deployment or measured contention requires it.
