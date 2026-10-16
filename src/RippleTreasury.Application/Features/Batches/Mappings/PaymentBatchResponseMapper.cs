using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Builds a batch response from its immutable instructions and current executions.</summary>
internal static class PaymentBatchResponseMapper
{
    /// <summary>Projects batch metadata, domain-calculated progress and currency-separated totals into the batch response.</summary>
    public static PaymentBatchResponseModel Map(BatchReadData data) => new(
        data.Batch.Id, data.Batch.ClientReference, data.Batch.CorrelationId, data.Batch.ReceivedAt,
        PaymentPolicy.BatchStatus(data.Executions.Select(x => x.Status)), data.Instructions.Length,
        CurrencyTotalsResponseMapper.Map(data.Instructions));
}
