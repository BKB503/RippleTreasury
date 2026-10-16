using RippleTreasury.Contracts;

namespace RippleTreasury.Application;

/// <summary>Read-only application boundary for individual payment evidence.</summary>
public interface IPaymentQueries
{
    /// <summary>Returns payment details with its durable attempts, audit events and actor/date history.</summary>
    Task<PaymentDetailResponseModel> Payment(Guid id, CancellationToken ct);
}
