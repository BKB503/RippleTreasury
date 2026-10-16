using RippleTreasury.Application;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

internal static class BusinessAuditWriter
{

    /// <summary>Adds an append-only business event to the current transaction, capturing the initiating actor or system.</summary>
    internal static void Audit(TreasuryDb db, Guid payment, Guid batch, Guid execution, string correlation, Guid? attempt,
        DateTime now, Transition transition, string systemActor, PaymentAuditAction action)
        => Audit(db, payment, batch, execution, correlation, attempt, now, transition, AuditActor.System(systemActor),
            systemActor == "recovery" ? "LeaseRecovery" : "DispatchWorker", action);

    /// <summary>Adds an append-only business event to the current transaction, capturing the initiating actor or system.</summary>
    internal static void Audit(TreasuryDb db, Guid payment, Guid batch, Guid execution, string correlation, Guid? attempt,
        DateTime now, Transition transition, AuditActor actor, string source, PaymentAuditAction action)
        => db.PaymentAuditEvents.Add(new()
        {
            Id = Guid.NewGuid(),
            PaymentId = payment,
            BatchId = batch,
            ExecutionId = execution,
            At = now,
            Action = action.ToString(),
            Before = transition.Before.ToString(),
            After = transition.After.ToString(),
            Version = transition.Version,
            Actor = actor.Id,
            ActorDisplayName = actor.DisplayName,
            ActorKind = actor.Kind,
            Source = source,
            Reason = transition.Reason,
            CorrelationId = correlation,
            AttemptId = attempt
        });
}
