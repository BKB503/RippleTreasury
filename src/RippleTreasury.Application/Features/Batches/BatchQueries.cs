using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Batch read use cases: ask the repository for data, then project domain-derived status and totals.</summary>
public class BatchQueries(IPaymentReadRepository repository) : IBatchQueries
{
    /// <summary>Loads one batch and maps domain-derived status and per-currency instruction totals.</summary>
    public async Task<PaymentBatchResponseModel> Batch(Guid id, CancellationToken ct) => PaymentBatchResponseMapper.Map(await repository.GetBatch(id, ct));

    /// <summary>Validates paging, filters batch summaries by status and returns the requested page.</summary>
    public async Task<PagedResponseModel<PaymentBatchResponseModel>> Batches(int page, int size, string? status, CancellationToken ct)
    {
        PaginationValidator.Validate(page, size);
        var data = await repository.GetBatches(null, null, ct);
        var views = data.Select(PaymentBatchResponseMapper.Map).Where(x => status is null || x.Status == status).ToArray();
        return new(views.Skip(checked((page - 1) * size)).Take(size).ToArray(), page, size, views.Length);
    }

    /// <summary>Returns a stable page of instructions with each payment's current execution state.</summary>
    public async Task<PagedResponseModel<PaymentResponseModel>> Payments(Guid batchId, int page, int size, CancellationToken ct)
    {
        PaginationValidator.Validate(page, size);
        var data = await repository.GetBatch(batchId, ct);
        var instructions = data.Instructions.OrderBy(x => x.Id).Skip(checked((page - 1) * size)).Take(size);
        return new(instructions.Select(x => PaymentResponseMapper.Map(data.Batch, x, data.Executions.Single(e => e.Id == x.Id))).ToArray(),
            page, size, data.Instructions.Length);
    }
}
