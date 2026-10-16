using static RippleTreasury.Infrastructure.BusinessAuditWriter;
using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

public partial class PaymentExecutionRepository(IDbContextFactory<TreasuryDb> factory, ITransactionFaultInjector? faultInjector = null, IAuditActorAccessor? actors = null) : IExecutionStore, IReconciliationStore
{

    /// <summary>Commits the due-date-gated lease, durable attempt and audit before any bank call.</summary>
    public async Task<WorkItem?> Claim(string owner, DateTime now, TimeSpan lease, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // SQLite serializes this short claim transaction; the network call happens after commit.
        var today = DateOnly.FromDateTime(now);
        var row = await (from e in db.PaymentExecutions
                         join p in db.PaymentInstructions on e.Id equals p.Id
                         where (e.Status == PaymentStatus.Ready || (e.Status == PaymentStatus.RetryScheduled && e.NextAttemptAt <= now)) && p.ExecutionDate <= today
                         orderby p.Id
                         select e).FirstOrDefaultAsync(ct);
        if (row is null)
            return null;
        var transition = row.Claim(row.Version, owner, now, lease);
        var attempt = new PaymentAttempt { Id = Guid.NewGuid(), PaymentId = row.Id, StartedAt = now, Actor = owner, ActorDisplayName = owner, ActorKind = "System", Source = "DispatchWorker" };
        db.PaymentAttempts.Add(attempt);
        var pRow = await db.PaymentInstructions.SingleAsync(x => x.Id == row.Id, ct);
        var b = await db.PaymentBatches.SingleAsync(x => x.Id == pRow.BatchId, ct);
        Audit(db, row.Id, b.Id, row.ExecutionId, b.CorrelationId, attempt.Id, now, transition, owner, PaymentAuditAction.SubmissionStarted);
        await db.SaveChangesAsync(ct);
        faultInjector?.BeforeCommit("claim");
        await tx.CommitAsync(ct);
        return new(pRow, row, attempt.Id, owner, b.CorrelationId, b.TraceParent, row.SubmissionCount);
    }

    /// <summary>Fences late workers and atomically commits the domain outcome, attempt, case and audit.</summary>
    public async Task Complete(WorkItem item, BankObservation observation, DateTime now, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var row = await db.PaymentExecutions.SingleAsync(x => x.Id == item.Instruction.Id, ct);
        if (row.Version != item.Execution.Version || row.Status != PaymentStatus.Submitting || row.LeaseOwner != item.LeaseOwner)
            throw new VersionConflictException();
        // Domain decides the transition; a late worker cannot overwrite a recovered lease.
        var transition = row.RecordObservation(observation, now);
        var a = await db.PaymentAttempts.SingleAsync(x => x.Id == item.AttemptId, ct);
        a.Complete(observation.Outcome, now);
        if (PaymentPolicy.RequiresReconciliation(row.Status))
        {
            var existing = await db.ReconciliationCases.SingleOrDefaultAsync(x => x.PaymentId == row.Id, ct);
            if (existing is null)
                db.ReconciliationCases.Add(new()
                {
                    Id = Guid.NewGuid(),
                    PaymentId = row.Id,
                    OpenedAt = now
                });
            else
            {
                existing.Reopen();
            }
        }
        Audit(db, row.Id, item.Instruction.BatchId, row.ExecutionId, item.CorrelationId, item.AttemptId, now, transition, item.LeaseOwner, PaymentAuditAction.SubmissionObserved);
        await db.SaveChangesAsync(ct);
        faultInjector?.BeforeCommit("complete");
        await tx.CommitAsync(ct);
    }


}
