using Microsoft.Extensions.Logging;

namespace RippleTreasury.Application;

/// <summary>Provides a common structured identifier scope for payment workflow logs.</summary>
internal static class WorkLogScope
{
    // Formatted scope values remain structured and are readable in the console provider.
    // Only identifiers are included: no beneficiary, account token or bank response body.

    /// <summary>Adds batch, payment, execution, attempt, correlation and provider identifiers to the operation's log scope.</summary>
    internal static IDisposable? Begin(ILogger logger, WorkItem item) => logger.BeginScope(
        "BatchId={BatchId} PaymentId={PaymentId} ExecutionId={ExecutionId} AttemptId={AttemptId} CorrelationId={CorrelationId} Provider={Provider} FundingAccountId={FundingAccountId} RoutingPolicyVersion={RoutingPolicyVersion}",
        item.Instruction.BatchId, item.Instruction.Id, item.Execution.ExecutionId,
        item.AttemptId, item.CorrelationId, item.Instruction.Provider,
        item.Instruction.Treasury?.FundingAccountId, item.Instruction.Treasury?.RoutingPolicyVersion);
}
