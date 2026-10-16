using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Maps an investigation's current state without executing a bank lookup or submission.</summary>
internal static class ReconciliationCaseResponseMapper
{
    /// <summary>Projects investigation status and version with its linked payment and correlation identifier.</summary>
    public static ReconciliationCaseResponseModel Map(ReconciliationCase item, string correlationId) =>
        new(item.Id, item.PaymentId, item.Status, item.Version, item.OpenedAt, correlationId);
}
