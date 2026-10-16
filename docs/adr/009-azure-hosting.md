# ADR 009 Container Apps + Bicep (design)

**Status:** Proposed (not deployed)

## Context

A production deployment needs managed compute, secrets, database backups and explicit release controls.

## Decision

Managed containers for API/worker, SWA for UI, PostgreSQL and managed identity.

## Alternatives and trade-offs

AKS adds orchestration burden; App Service simpler API hosting; Functions require careful durable orchestration; Terraform cross-cloud.

## Consequences and revisit

Platform standards, network constraints or sustained cost/scale comparison. Preserve compatible persisted instructions and execution identities during change. An architectural rollback cannot reverse a bank transfer or remove audit evidence.
