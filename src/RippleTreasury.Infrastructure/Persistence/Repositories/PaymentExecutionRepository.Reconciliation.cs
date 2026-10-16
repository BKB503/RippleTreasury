using static RippleTreasury.Infrastructure.BusinessAuditWriter;
using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

public partial class PaymentExecutionRepository
{

    /// <summary>Commits a version-checked operator lookup attempt and its actor before querying the provider.</summary>
    public async Task<WorkItem> BeginRecheck(Guid caseId, int expectedVersion, Guid commandId, DateTime now, CancellationToken ct)
    {
        if (commandId == Guid.Empty)
            throw new DomainException("Command ID required.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var c = await db.ReconciliationCases.SingleOrDefaultAsync(x => x.Id == caseId, ct) ?? throw new MissingException();
        if (await db.PaymentAttempts.AnyAsync(x => x.CommandId == commandId, ct))
            throw new ConflictException("Recheck command already recorded; reload case.");
        ReconciliationRules.EnsureCanRecheck(c.Status, c.Version, expectedVersion);
        var row = await db.PaymentExecutions.SingleAsync(x => x.Id == c.PaymentId, ct);
        var p = await db.PaymentInstructions.SingleAsync(x => x.Id == row.Id, ct);
        var b = await db.PaymentBatches.SingleAsync(x => x.Id == p.BatchId, ct);
        var actor = actors?.Current ?? AuditActor.System("system-reconciliation");
        var a = new PaymentAttempt { Id = Guid.NewGuid(), PaymentId = row.Id, StartedAt = now, Kind = "Lookup", CommandId = commandId, Actor = actor.Id, ActorDisplayName = actor.DisplayName, ActorKind = actor.Kind, Source = "Reconciliation" };
        db.PaymentAttempts.Add(a);
        c.BeginLookup(expectedVersion, a.Id, now);
        Audit(db, row.Id, b.Id, row.ExecutionId, b.CorrelationId, a.Id, now, new(row.Status, row.Status, row.Version, "Provider status recheck started"), actor, "Reconciliation", PaymentAuditAction.ReconciliationStarted);
        await db.SaveChangesAsync(ct);
        faultInjector?.BeforeCommit("recheck-start");
        await tx.CommitAsync(ct);
        return new(p, row, a.Id, "reconciliation", b.CorrelationId, b.TraceParent, row.SubmissionCount);
    }

    /// <summary>Commits only the current lookup result, preserving the original initiating actor and bank identity.</summary>
    public async Task FinishRecheck(WorkItem item, BankObservation observation, DateTime now, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var row = await db.PaymentExecutions.SingleAsync(x => x.Id == item.Instruction.Id, ct);
        var c = await db.ReconciliationCases.SingleAsync(x => x.PaymentId == row.Id, ct);
        if (row.Version != item.Execution.Version || c.ActiveAttemptId != item.AttemptId)
            throw new VersionConflictException();
        var t = row.RecordObservation(observation, now);
        c.CompleteLookup(item.AttemptId, row.Status);
        var a = await db.PaymentAttempts.SingleAsync(x => x.Id == item.AttemptId, ct);
        a.Complete(observation.Outcome, now);
        var actor = new AuditActor(a.Actor, a.ActorDisplayName, a.ActorKind);
        Audit(db, row.Id, item.Instruction.BatchId, row.ExecutionId, item.CorrelationId, a.Id, now, t, actor, "Reconciliation", PaymentAuditAction.ReconciliationCompleted);
        await db.SaveChangesAsync(ct);
        faultInjector?.BeforeCommit("recheck-finish");
        await tx.CommitAsync(ct);
    }
}
