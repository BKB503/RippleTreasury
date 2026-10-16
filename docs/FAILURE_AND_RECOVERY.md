# Failure and recovery runbook

## Safety rule

Unknown does not mean rejected. Do not clear Submitting rows, delete attempts/cases or set a payment to Ready to make the queue move. Expired leases never authorise a new financial submission. Reconciliation looks up the stable ExecutionId and records provider evidence.

## Failure matrix

| Failure point | Durable state | Recovery | Verification |
|---|---|---|---|
| Invalid line | Nothing accepted | Correct request; whole batch rejected | Indexed 400 validation test |
| Intake transaction failure | No partial rows/audit/key | Replay same idempotency key | Injected accept rollback test |
| Intake commit but HTTP response lost | One batch + idempotency record | Repeat same key/payload; obtain original ID | Replay/concurrent intake tests |
| Claim before commit fails | Ready, no attempt effect | Another claim may proceed | Atomic claim transaction |
| Claim commits, process crashes before send | Submitting + attempt | Lease expires → Unknown; lookup; manual review if inconclusive | Crash-before-send test |
| Bank definitive rejection | Rejected + observed attempt + audit | Correct with new instruction/ref; no retry of original | 500-payment independent outcome test |
| Authoritative no-execution transient | RetryScheduled + proof | Due attempt under bounded domain policy, same execution identity | Safe transient retry test |
| POST timeout/disconnect/500/504 | Unknown, or stale Submitting if local save failed | Provider GET lookup; no automatic POST retry | Actual resilience test; Playwright lost response |
| Bank accepts, outcome commit fails | Submitting + durable attempt, bank registry accepted | Restart, expire lease, reconcile | Fault injection proves one execution |
| Late completion after recovery | Higher execution version | Reject stale writer; inspect case | Version/lease test |
| Concurrent worker pickup | One durable owner | Other worker receives no eligible claim | Eight competing claim test |
| GET transient failure | QueryingProvider or uncertainty | Bounded GET retries; preserve evidence; repeat safe lookup | Actual resilience GET test |
| Recheck command duplicate/stale | Existing attempt/case version | 409, reload evidence; no financial resend | Duplicate/stale recheck tests |
| Process crashes during recheck | Lookup lease + attempt | Lease expires → AwaitingEvidence; safe recheck | Durable lookup lease path; see tests/known evidence |
| Provider NotFound/unknown | ReconciliationRequired + manual case | Obtain authoritative evidence; no generic override | Crash-before-send/inconclusive lookup test |
| Audit persistence/transaction fails | Business state rolled back with audit | Preserve durable attempt, repair DB, reconcile | Injected complete failure test |
| Database unavailable | Readiness unhealthy, worker logs infrastructure error | Restore connectivity; inspect ambiguous attempts | Health endpoint and durable-state design |

Tests assert no second mock execution for accepted-but-response-lost and accepted-but-result-save-failed. They do not prove real-bank guarantees.

## Local reproducible investigation

1. Run the unified startup command from README; generate and submit a mixed-outcome batch from the Process batch page.
2. In Swagger or GET batch payments, inspect independent Accepted, Rejected and OutcomeUnknown statuses.
3. Open Reconciliation, Inspect the uncertain case, review execution/attempt IDs, correlation and audit.
4. Confirm Recheck bank status. Backend records lookup intent, performs GET, saves authoritative evidence and resolves the case. Accepted remains distinct from settlement.
5. Replaying the batch returns the original ID. Rechecking with a stale If-Match returns 409; reload before another operation.

For crash recovery, the automated test stores an attempt, simulates bank execution and a failed result commit, advances an injected clock beyond the lease and constructs a new processor. It then reconciles and asserts one execution. This is deterministic process-state restart simulation, not an OS-kill chaos test. An actual interrupted local worker can be restarted normally; wait beyond 30 seconds before examining recovery. Never simulate recovery by changing persisted financial status manually.

For a separate worker, disable Processing:EmbeddedWorker on API and provide the same absolute Database:Path to both hosts. The standalone worker needs a running Development API for the mock provider HTTP endpoints. Keep one recommended dispatcher against SQLite.

## Operational alerts (proposed, not deployed)

| Alert | Initial hypothesis | Response |
|---|---|---|
| Unknown/case age | Any uncertain case >15 minutes in demo; agree production SLA | Treasury operations reviews external evidence and correlation |
| Backlog age/progress | Ready/due oldest age >5 minutes and no outcomes in 2 minutes | Check worker, due dates, DB locks, provider capacity; do not bulk resend |
| Provider transport errors | >5% classified errors over 5 minutes, subject to volume floor | Review circuit breaker/status availability and provider incident |
| Persistence/audit failure | Any failed commit during result persistence | Page service/data owner; preserve attempts; reconcile after repair |
| DB readiness/telemetry outage | 3 failed probes / no expected spans for 5 minutes | Repair infrastructure; inspect work progress independently |
| Idempotency conflicts | Sustained spike above client baseline | Review client key reuse, payload mismatch and tenant controls |

Thresholds are starting design assumptions, not measured SLOs. Configure App Insights/Log Analytics alerts and link this runbook. No alert should trigger uncertain-payment submission.

## Production incident and restore procedure

Preserve audit, execution keys, attempt records and bank evidence. Limit dispatch for the affected provider while allowing unrelated work when safe. Identify the external execution by the stable key/reference and obtain authoritative provider status. Document actor, incident, evidence and causation; production manual action requires authorisation and maker/checker. No such manual override API is implemented.

A restored database may predate real bank execution. Before enabling dispatch, fence old workers, reconcile every potentially submitted instruction against provider evidence and compare immutable bank keys; do not replay a stale backup as if it were a new queue. Restore to an isolated environment first, verify constraints/audit/history, then follow approved continuity procedure. Agree business RPO/RTO and prove them through drills. Backups and logs cannot reverse bank effects.

## Known limits

SQLite tests are local-provider evidence. No live bank, production restore, HA, disaster recovery, hosted alerts, cryptographic audit archival or multi-region failover has been verified. Inconclusive cases remain visible for review. The demo intentionally exposes no arbitrary force-resolution or unsafe-retry button.
