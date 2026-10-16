# ADR 005 Idempotency and fault verification

**Status:** Accepted (local implementation)

## Context

Client requests, workers and observations may repeat. A network failure or failed database commit does not prove that a bank did nothing.

## Decision

Use tenant-scoped client keys and payload hashes at intake, a stable provider execution key, durable attempt/lease records and guarded domain transitions. Ambiguous submission outcomes require status evidence before another submission is eligible.

The optional ITransactionFaultInjector is an Infrastructure test seam invoked immediately before commit. Tests supply a throwing implementation to prove that payment state and audit roll back together, then verify restart/reconciliation without duplicate bank execution. Production registers no injector; there is no no-op class, fault scheduler or additional retry mechanism.

SimulatedBankExecution records provider-side executions independently so tests can assert the actual simulated financial execution count rather than infer safety from the API response.

## Alternatives and trade-offs

Messaging cannot guarantee exactly-once external financial effects. Removing fault injection would leave important commit-boundary failures hard to reproduce; adding a production fault framework would be unnecessary. Conservative uncertainty can require operator investigation.

## Consequences and revisit

Never register a throwing test injector in deployed hosts. Keep fault hooks outside Domain and separate from Polly/provider retry policy. Stronger real-bank guarantees require a verified bank idempotency and authoritative status contract.


## Incremental treasury extension

Legacy request and bank fingerprint canonical shapes are retained exactly. Enhanced requests append treasury intent to their request hash; the provider payload fingerprint includes its accepted treasury snapshot. New nullable fields are not backfilled. Same-key changed funding/beneficiary intent conflicts. Reconciliation retains the original provider key regardless of new intake capabilities.


Enhanced replay is checked from committed tenant/key/hash evidence before re-evaluating current capability availability. The provider payload fingerprint is not used for this lookup: BatchRequestFingerprint preserves the client-intent hash, while MockSubmission.Fingerprint protects the provider execution payload. A disabled bank can block new intake but cannot turn a matching accepted replay into a new routing decision. The final intake transaction remains the concurrency/idempotency authority.
