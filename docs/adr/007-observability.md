# ADR 007 Business audit and technical observability

**Status:** Accepted locally; production identity and Azure operations remain design

## Context

A durable financial audit and sampled technical telemetry answer different questions.

## Decision

Persist append-only PaymentAuditEvents in the same transaction as execution/case changes. Snapshot action, UTC timestamp, reason, version, correlation and actor identity. ILogger scopes, OpenTelemetry traces/metrics and health endpoints provide technical diagnostics without secrets or full account details.

Resolve known X-Demo-User profiles only in Development. Persist the initiating recheck actor on PaymentAttempt so completion keeps that identity after the request context changes. Worker and recovery actions use System identities. PaymentHistoryProjection sits with payment response mappers and exposes creation/update/reconciliation dates and actors.

## Alternatives and trade-offs

Logs alone can be sampled or lost and cannot establish a business commit. Copying arbitrary user names from payloads would permit forged audit history. Known fictional operators demonstrate attribution but are not authentication.

## Consequences and revisit

Production requires validated Entra subject/tenant claims, RBAC, maker/checker controls, retention and tamper-evidence decisions. Exporter/alert configuration and Azure operation require deployment verification. Do not invent missing historical provenance or infer settlement from an Accepted status.


## Incremental treasury extension

Treasury intake audit reason contains accepted funding ID, provider, routing version and decision reason. Attempts expose immutable instruction context through read projection. Activities/log scopes include safe source IDs and policy version, never full beneficiary account tokens; routing rejections produce stable codes and correlated operational logs without creating invalid financial audit rows.
