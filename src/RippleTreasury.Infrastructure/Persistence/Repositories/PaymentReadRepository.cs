using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

/// <summary>Loads consistent, detached Domain data. Application owns query validation and API projection.</summary>
public class PaymentReadRepository(IDbContextFactory<TreasuryDb> factory) : IPaymentReadRepository
{
    /// <summary>Reads consistent detached batch/instruction/execution snapshots for reporting.</summary>
    public async Task<BatchReadData[]> GetBatches(DateTime? from, DateTime? to, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var batches = await db.PaymentBatches.AsNoTracking().Where(x => (from == null || x.ReceivedAt >= from) && (to == null || x.ReceivedAt <= to))
            .OrderByDescending(x => x.ReceivedAt).ThenBy(x => x.Id).ToArrayAsync(ct);
        var ids = batches.Select(x => x.Id).ToArray();
        var instructions = await db.PaymentInstructions.AsNoTracking().Where(x => ids.Contains(x.BatchId)).ToArrayAsync(ct);
        var paymentIds = instructions.Select(x => x.Id).ToArray();
        var executions = await db.PaymentExecutions.AsNoTracking().Where(x => paymentIds.Contains(x.Id)).ToArrayAsync(ct);
        return batches.Select(batch => new BatchReadData(batch, instructions.Where(x => x.BatchId == batch.Id).ToArray(),
            executions.Where(x => instructions.Any(p => p.Id == x.Id && p.BatchId == batch.Id)).ToArray())).ToArray();
    }

    /// <summary>Loads one batch and its members within a read transaction.</summary>
    public async Task<BatchReadData> GetBatch(Guid id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var batch = await db.PaymentBatches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new MissingException();
        var instructions = await db.PaymentInstructions.AsNoTracking().Where(x => x.BatchId == id).ToArrayAsync(ct);
        var ids = instructions.Select(x => x.Id).ToArray();
        var executions = await db.PaymentExecutions.AsNoTracking().Where(x => ids.Contains(x.Id)).ToArrayAsync(ct);
        return new(batch, instructions, executions);
    }

    /// <summary>Loads one payment and its evidence consistently without tracking mutable EF entities.</summary>
    public async Task<PaymentReadData> GetPayment(Guid id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        return await LoadPayment(db, id, ct);
    }

    /// <summary>Reads investigation metadata with optional status/batch scoping.</summary>
    public async Task<(ReconciliationCase Case, string CorrelationId)[]> GetCases(string? status, Guid? batchId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var cases = await (from item in db.ReconciliationCases.AsNoTracking()
                           join instruction in db.PaymentInstructions on item.PaymentId equals instruction.Id
                           join batch in db.PaymentBatches on instruction.BatchId equals batch.Id
                           where (status == null || item.Status == status) && (batchId == null || batch.Id == batchId)
                           orderby item.OpenedAt, item.Id
                           select new { Case = item, batch.CorrelationId }).ToArrayAsync(ct);
        return cases.Select(x => (x.Case, x.CorrelationId)).ToArray();
    }

    /// <summary>Loads case and payment evidence within the same read transaction.</summary>
    public async Task<(ReconciliationCase Case, PaymentReadData Payment)> GetCase(Guid id, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        // Case version, financial state and evidence are read from the same local snapshot.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var item = await db.ReconciliationCases.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new MissingException();
        return (item, await LoadPayment(db, item.PaymentId, ct));
    }

    /// <summary>Loads accepted intent, execution, ordered attempts and append-only audit for the enclosing read transaction.</summary>
    private static async Task<PaymentReadData> LoadPayment(TreasuryDb db, Guid id, CancellationToken ct)
    {
        var instruction = await db.PaymentInstructions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new MissingException();
        var execution = await db.PaymentExecutions.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        var batch = await db.PaymentBatches.AsNoTracking().SingleAsync(x => x.Id == instruction.BatchId, ct);
        var attempts = await db.PaymentAttempts.AsNoTracking().Where(x => x.PaymentId == id).OrderBy(x => x.StartedAt).ThenBy(x => x.Id).ToArrayAsync(ct);
        var audit = await db.PaymentAuditEvents.AsNoTracking().Where(x => x.PaymentId == id).OrderBy(x => x.At).ThenBy(x => x.Version).ToArrayAsync(ct);
        return new(batch, instruction, execution, attempts, audit);
    }
}
