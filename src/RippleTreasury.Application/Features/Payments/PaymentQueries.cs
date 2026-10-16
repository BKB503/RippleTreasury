using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Builds a payment evidence response from a consistent repository snapshot.</summary>
public class PaymentQueries(IPaymentReadRepository repository) : IPaymentQueries
{
    /// <summary>Loads a consistent payment snapshot and returns its attempts, business audit and actor history.</summary>
    public async Task<PaymentDetailResponseModel> Payment(Guid id, CancellationToken ct) => PaymentDetailResponseMapper.Map(await repository.GetPayment(id, ct));
}
