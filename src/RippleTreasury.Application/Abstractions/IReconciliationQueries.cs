using RippleTreasury.Contracts;

namespace RippleTreasury.Application;

/// <summary>Read-only application boundary for reconciliation investigations.</summary>
public interface IReconciliationQueries
{
    /// <summary>Returns paged reconciliation investigations with optional status and batch filters.</summary>
    Task<PagedResponseModel<ReconciliationCaseResponseModel>> Cases(int page, int size, string? status, CancellationToken ct, Guid? batchId = null);

    /// <summary>Returns one reconciliation investigation with its linked payment evidence.</summary>
    Task<ReconciliationCaseDetailResponseModel> Case(Guid id, CancellationToken ct);
}
