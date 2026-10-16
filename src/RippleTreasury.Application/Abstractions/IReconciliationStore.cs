using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Durable version-checked provider-lookup workflow persistence boundary.</summary>
public interface IReconciliationStore
{
    /// <summary>Validates the case version and command identity, then commits a leased lookup attempt with its initiating actor.</summary>
    Task<WorkItem> BeginRecheck(Guid caseId, int expectedVersion, Guid commandId, DateTime now, CancellationToken ct);

    /// <summary>Applies provider evidence through domain transitions and commits payment, case, attempt and audit together.</summary>
    Task FinishRecheck(WorkItem item, BankObservation observation, DateTime now, CancellationToken ct);
}
