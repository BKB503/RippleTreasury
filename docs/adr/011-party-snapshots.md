# ADR 012 Purpose-specific immutable party snapshots

**Status:** Accepted (local implementation)

## Context

Vendor disbursements, intercompany transfers and loan repayments must explain who pays whom and why. A purpose label alone is insufficient.

## Decision

Capture payer/payee reference, legal name, country, kind, corporate group and opaque account token with each accepted instruction, plus an invoice, transfer or loan business reference. Domain validates purpose and group relationships.

Two PaymentPartySnapshot entities per enriched instruction are saved in its intake transaction and protected from updates/deletes. PartyDetails is their validated value object. PaymentPartyRequestModel/PaymentPartiesRequestModel accept input; separate response models expose the accepted snapshots through PaymentResponseMapper.

## Alternatives and trade-offs

A mutable company/vendor master alone would alter the apparent historical instruction when names or accounts change. Snapshot storage duplicates selected reference details intentionally to preserve accepted intent. Transport request/response records are not additional persisted entity sets.

## Consequences and revisit

Verified company/account master data, approval and beneficiary validation remain production integrations. Optional party fields retain existing demo v1 compatibility; production admission should require verified references. Do not silently rewrite accepted snapshots when master data changes.


## Incremental treasury extension

Enhanced beneficiary reference/token and source owner/country must match existing party snapshots. An explicit beneficiary identifier type and optional BIC syntax refine the model; only fictional TokenisedReference is supported. A verified beneficiary/account master, real IBAN validation and liquidity/FX remain future integrations.
