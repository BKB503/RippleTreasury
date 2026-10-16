# ADR 006 API models and independent versions

**Status:** Accepted (local implementation; event evolution is design only)

## Context

HTTP compatibility, entity concurrency and future events are different contracts. Names such as Input or View obscure which direction a model travels.

## Decision

Keep API routes under /api/v1. Contracts/Http/V1 groups models by feature and then Requests or Responses, with one type per file. Use descriptive RequestModel/ResponseModel suffixes, for example CreatePaymentBatchRequestModel, PaymentInstructionRequestModel, PaymentBatchResponseModel and PaymentDetailResponseModel.

Request party models represent supplied intent; response party models represent accepted snapshots. Domain entities are never returned directly. Application feature mappers compose response models, while controllers remain thin. Renaming CLR types does not change JSON property names, routes or request semantics; OpenAPI schema names intentionally reflect the clearer types.

Keep concurrency integers on executions/cases separate from API v1 and processing policy mock-v1. The future event envelope has its own schema version; no publisher or runtime event contract is implemented yet.

## Alternatives and trade-offs

Reusing input DTOs in responses reduces classes but couples validation and public output evolution. Distinct directional contracts are a deliberate transport boundary, not duplicate database/domain entities. Header API versioning is possible but less discoverable for this demo.

## Consequences and revisit

Test serialization and the React flow after contract changes. Breaking wire changes require an explicit API compatibility decision. Introduce compiled integration event models with the first actual publisher/consumer and compatibility tests; see ADR 008.


## Incremental treasury extension

The optional treasury request/response object, funding/capability query endpoints and indexed validationCodes are additive v1 extensions. Existing route/JSON names and RequestModel/ResponseModel folders remain. executionDate is not renamed. Omitted treasury explicitly uses the old compatibility path; production admission must not rely on that path for funding validation.
