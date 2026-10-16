using static RippleTreasury.Infrastructure.BusinessAuditWriter;
using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

public class BatchIntakeRepository(IDbContextFactory<TreasuryDb> factory, TimeProvider clock, ITransactionFaultInjector? faultInjector = null, IAuditActorAccessor? actors = null) : IBatchIntakeStore
{

    /// <summary>Reads immutable tenant/key/hash evidence without changing accepted routing or payment state.</summary>
    public async Task<PaymentBatchReceiptResponseModel?> FindReplay(string tenant, string key, string hash, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var prior = await db.IdempotencyRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Tenant == tenant && x.Key == key, ct);
        if (prior is null) return null;
        if (prior.Hash != hash) throw new ConflictException("Idempotency key already used with a different payload.");
        return new(prior.BatchId, true);
    }

    /// <summary>Commits batch membership, immutable intent, execution IDs, routing audit and idempotency together, or safely replays the prior batch.</summary>
    public async Task<PaymentBatchReceiptResponseModel> Accept(string tenant, string key, string hash, string reference, string correlation, string? traceParent, PaymentInstruction[] instructions, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        // Replay is decided before creating any instruction or audit record.
        var prior = await db.IdempotencyRequests.SingleOrDefaultAsync(x => x.Tenant == tenant && x.Key == key, ct);
        if (prior is not null)
        {
            if (prior.Hash != hash)
                throw new ConflictException("Idempotency key already used with a different payload.");
            return new(prior.BatchId, true);
        }
        if (await db.PaymentBatches.AnyAsync(x => x.Tenant == tenant && x.ClientReference == reference, ct))
            throw new ConflictException("Client batch reference already accepted.");
        var now = clock.GetUtcNow().UtcDateTime;
        var id = instructions[0].BatchId;
        db.PaymentBatches.Add(new()
        {
            Id = id,
            Tenant = tenant,
            ClientReference = reference,
            CorrelationId = correlation,
            TraceParent = traceParent,
            ReceivedAt = now
        });
        db.IdempotencyRequests.Add(new()
        {
            Id = Guid.NewGuid(),
            Tenant = tenant,
            Key = key,
            Hash = hash,
            BatchId = id
        });
        // Membership, intent snapshots, execution IDs and initial audit share one transaction.
        foreach (var p in instructions)
        {
            db.PaymentInstructions.Add(p);
            var e = new PaymentExecution(p.Id, Guid.NewGuid());
            db.PaymentExecutions.Add(e);
            Audit(db, p.Id, id, e.ExecutionId, correlation, null, now, new(PaymentStatus.Ready, PaymentStatus.Ready, 0, p.Treasury is null ? "Instruction accepted" :
                $"Instruction accepted; funding={p.Treasury.FundingAccountId}; provider={p.Provider}; routing={p.Treasury.RoutingPolicyVersion}; {p.Treasury.RoutingReason}"), actors?.Current ?? AuditActor.System("system-client"), "BatchIntake", PaymentAuditAction.InstructionCreated);
        }
        await db.SaveChangesAsync(ct);
        faultInjector?.BeforeCommit("accept");
        await tx.CommitAsync(ct);
        return new(id, false);
    }
}
