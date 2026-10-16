using RippleTreasury.Domain;
using RippleTreasury.Contracts;

namespace RippleTreasury.Application;

/// <summary>Builds payment actor and date history from durable evidence without changing accepted instructions.</summary>
internal static class PaymentHistoryProjection
{

    /// <summary>Derives creation, last-update and latest reconciliation dates and actors from durable audit and attempt records.</summary>
    public static PaymentHistoryResponseModel Create(PaymentBatch batch, PaymentAuditEvent[] audit, PaymentAttempt[] attempts)
    {
        var created = audit.First(x => x.Version == 0);
        var last = audit.Last();
        var recheck = attempts.LastOrDefault(x => x.Kind == "Lookup");
        return new(batch.ReceivedAt, Actor(created), last.At, Actor(last), recheck?.StartedAt, recheck?.CompletedAt,
            recheck is null ? null : new(recheck.Actor, recheck.ActorDisplayName, recheck.ActorKind));
    }

    /// <summary>Projects the identity, display name and kind captured on a persisted business audit event.</summary>
    private static AuditActorResponseModel Actor(PaymentAuditEvent row) => new(row.Actor, row.ActorDisplayName, row.ActorKind);
}
