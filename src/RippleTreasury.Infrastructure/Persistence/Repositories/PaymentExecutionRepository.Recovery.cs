using static RippleTreasury.Infrastructure.BusinessAuditWriter;
using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

public partial class PaymentExecutionRepository
{

    /// <summary>Recovers stale submissions/lookups with conservative audited outcomes; never reroutes or blindly resubmits.</summary>
    public async Task<int> RecoverExpired(DateTime now, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // A stale lease proves interrupted processing, not that the bank did not execute.
        var rows = await db.PaymentExecutions.Where(x => x.Status == PaymentStatus.Submitting && x.LeaseUntil <= now).ToArrayAsync(ct);
        foreach (var row in rows)
        {
            var t = row.RecoverExpiredLease(now);
            var a = await db.PaymentAttempts.Where(x => x.PaymentId == row.Id && x.Kind == "Submit" && x.CompletedAt == null).SingleAsync(ct);
            a.Complete(BankOutcome.Unknown, now);
            var p = await db.PaymentInstructions.SingleAsync(x => x.Id == row.Id, ct);
            var b = await db.PaymentBatches.SingleAsync(x => x.Id == p.BatchId, ct);
            // A safe retry may already have a resolved case. Reuse it after a later crash.
            var existingCase = await db.ReconciliationCases.SingleOrDefaultAsync(x => x.PaymentId == row.Id, ct);
            if (existingCase is null)
                db.ReconciliationCases.Add(new() { Id = Guid.NewGuid(), PaymentId = row.Id, OpenedAt = now });
            else
                existingCase.Reopen();
            Audit(db, row.Id, b.Id, row.ExecutionId, b.CorrelationId, a.Id, now, t, "recovery", PaymentAuditAction.SubmissionLeaseExpired);
        }
        var stale = await db.ReconciliationCases.Where(x => x.Status == "QueryingProvider" && x.LeaseUntil <= now).ToArrayAsync(ct);
        foreach (var c in stale)
        {
            var a = await db.PaymentAttempts.SingleAsync(x => x.Id == c.ActiveAttemptId, ct);
            c.RecoverLookup(now);
            a.Complete(BankOutcome.Unknown, now);
            var payment = await db.PaymentInstructions.SingleAsync(x => x.Id == c.PaymentId, ct);
            var batch = await db.PaymentBatches.SingleAsync(x => x.Id == payment.BatchId, ct);
            var execution = await db.PaymentExecutions.SingleAsync(x => x.Id == c.PaymentId, ct);
            Audit(db, execution.Id, batch.Id, execution.ExecutionId, batch.CorrelationId, a.Id, now, new(execution.Status, execution.Status, execution.Version, "Expired provider lookup lease: awaiting evidence"), "recovery", PaymentAuditAction.ReconciliationLeaseExpired);
        }
        await db.SaveChangesAsync(ct);
        faultInjector?.BeforeCommit("recover");
        await tx.CommitAsync(ct);
        return rows.Length;
    }
}
