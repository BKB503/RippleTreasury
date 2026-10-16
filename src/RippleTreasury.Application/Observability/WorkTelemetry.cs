using System.Diagnostics;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Restores persisted trace context for background dispatch and reconciliation activities.</summary>
internal static class WorkTelemetry
{

    /// <summary>Continues the persisted W3C trace context and tags the activity with non-sensitive payment identifiers.</summary>
    internal static Activity? Start(string name, WorkItem item)
    {
        ActivityContext.TryParse(item.TraceParent, null, out var parent);
        var activity = Telemetry.Activities.StartActivity(name, ActivityKind.Internal, parent);
        activity?.SetTag("payment.id", item.Instruction.Id);
        activity?.SetTag("execution.id", item.Execution.ExecutionId);
        activity?.SetTag("correlation.id", item.CorrelationId);
        activity?.SetTag("attempt.id", item.AttemptId);
        activity?.SetTag("funding.account.id", item.Instruction.Treasury?.FundingAccountId);
        activity?.SetTag("routing.policy.version", item.Instruction.Treasury?.RoutingPolicyVersion);
        activity?.SetTag("payment.currency", item.Instruction.CurrencyCode);
        activity?.SetTag("payment.destination.country", item.Instruction.DestinationCountry);
        return activity;
    }
}
