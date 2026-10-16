using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Read-only case navigation. Rechecks remain explicit audited commands.</summary>
public class ReconciliationQueries(IPaymentReadRepository repository) : IReconciliationQueries
{
    /// <summary>Returns a validated page of investigations, optionally scoped to status and batch.</summary>
    public async Task<PagedResponseModel<ReconciliationCaseResponseModel>> Cases(int page, int size, string? status, CancellationToken ct, Guid? batchId = null)
    {
        PaginationValidator.Validate(page, size);
        var cases = await repository.GetCases(status, batchId, ct);
        return new(cases.Skip(checked((page - 1) * size)).Take(size).Select(x => ReconciliationCaseResponseMapper.Map(x.Case, x.CorrelationId)).ToArray(),
            page, size, cases.Length);
    }

    /// <summary>Loads an investigation and its consistent payment evidence for operator review.</summary>
    public async Task<ReconciliationCaseDetailResponseModel> Case(Guid id, CancellationToken ct)
    {
        var data = await repository.GetCase(id, ct);
        return new(ReconciliationCaseResponseMapper.Map(data.Case, data.Payment.Batch.CorrelationId), PaymentDetailResponseMapper.Map(data.Payment));
    }
}
