using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Composes the payment, durable attempts, audit timeline and actor history for a detail response.</summary>
internal static class PaymentDetailResponseMapper
{
    /// <summary>Composes the payment response, ordered attempts, audit timeline and derived actor/date history.</summary>
    public static PaymentDetailResponseModel Map(PaymentReadData data) => new(
        PaymentResponseMapper.Map(data.Batch, data.Instruction, data.Execution),
        data.Attempts.Select(x => new PaymentAttemptResponseModel(x.Id, x.PaymentId, x.StartedAt,
            x.CompletedAt, x.Outcome, x.Kind, data.Instruction.Provider, data.Instruction.Treasury?.FundingAccountId,
            data.Instruction.ExecutionDate, data.Instruction.Treasury?.RoutingPolicyVersion)).ToArray(),
        data.Audit.Select(x => new PaymentAuditEventResponseModel(x.Id, x.PaymentId, x.At, x.Before, x.After,
            x.Version, x.Actor, x.Reason, x.CorrelationId, x.AttemptId, x.ActorDisplayName, x.ActorKind,
            x.Source, x.PolicyVersion, x.Action)).ToArray(),
        PaymentHistoryProjection.Create(data.Batch, data.Audit, data.Attempts));
}
