# ADR 002 Domain entities and financial rules

**Status:** Accepted (local implementation)

## Context

Money precision, immutable intent and evidence-based transitions must have one implementation, independent of storage and transport.

## Decision

Domain owns Money, PartyDetails, accepted PaymentInstruction, PaymentExecution, PaymentAttempt and ReconciliationCase behavior. Private state changes enforce version and transition rules. EF maps these same entities through Infrastructure configuration classes; there are no Row classes or copy-back models.

PartyDetails is a value object without persistence identity. PaymentPartySnapshot is an identified immutable snapshot attached to an accepted instruction. HTTP request/response records are transport models, not additional domain entities. BatchReadData and PaymentReadData only group detached entities for consistent reads.

SimulatedBankExecution is an Infrastructure-only record of the simulated bank's execution ledger. It is deliberately separate from Treasury's PaymentExecution and demonstrates whether one provider key produced a second financial effect.

## Alternatives and trade-offs

CRUD status setters would bypass safety rules. Mapping a separate database entity graph would isolate an incompatible schema but adds conversion and drift risk here. Direct EF mapping requires materialization constructors/private setters and explicit configuration without adding EF dependencies to Domain.

## Consequences and revisit

Change financial behavior through domain methods and tests. Keep database and JSON concerns outside Domain. Revisit separate persistence models only for a demonstrated incompatible provider/legacy requirement, not as a default layer pattern.


## Incremental treasury extension

FundingAccount adds tenant/company-owned source reference data without balances. BeneficiaryAccount validates typed fictional tokens; TreasuryPaymentDetails is an immutable owned snapshot on PaymentInstruction. Funding currency must match instructed currency. BankCapability enforces funding-bank/account and corridor eligibility. Existing payment transitions are unchanged.
