using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Provider boundary for one payment submission and safe status-evidence lookup.</summary>
public interface IBankGateway
{
    /// <summary>Submits one claimed instruction using its stable execution identity; ambiguous transport results must not trigger a blind retry.</summary>
    Task<BankObservation> Submit(WorkItem item, CancellationToken ct);

    /// <summary>Queries provider evidence for the existing execution identity without submitting a payment.</summary>
    Task<BankObservation> Lookup(WorkItem item, CancellationToken ct);
}
