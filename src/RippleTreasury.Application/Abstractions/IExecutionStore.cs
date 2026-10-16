using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Durable dispatch, result-commit and crash-recovery persistence boundary.</summary>
public interface IExecutionStore
{
    /// <summary>Durably claims one eligible execution with its lease, attempt and audit before a bank call can begin.</summary>
    Task<WorkItem?> Claim(string owner, DateTime now, TimeSpan lease, CancellationToken ct);

    /// <summary>Atomically records the version-checked domain outcome, attempt completion, reconciliation case and audit.</summary>
    Task Complete(WorkItem item, BankObservation observation, DateTime now, CancellationToken ct);

    /// <summary>Recovers expired submission and lookup leases with audited conservative outcomes, without blindly resubmitting.</summary>
    Task<int> RecoverExpired(DateTime now, CancellationToken ct);
}
