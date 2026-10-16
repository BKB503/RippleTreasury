using Microsoft.EntityFrameworkCore;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Integration.Tests;

/// <summary>Composite adapter for test harnesses. Production registers each narrow port separately.</summary>
public class PaymentStoreHarness : IBatchIntakeStore, IExecutionStore, IReconciliationStore, IBatchQueries, IPaymentQueries, IReconciliationQueries, IReportingQueries
{
    private readonly IBatchIntakeStore intake;
    private readonly PaymentExecutionRepository execution;
    private readonly BatchQueries batches;
    private readonly PaymentQueries payments;
    private readonly ReconciliationQueries reconciliation;
    private readonly ReportingQueries reporting;

    public PaymentStoreHarness(IDbContextFactory<TreasuryDb> factory, ITransactionFaultInjector fault, TimeProvider clock, IAuditActorAccessor? actors = null)
    {
        intake = new BatchIntakeRepository(factory, clock, fault, actors);
        execution = new PaymentExecutionRepository(factory, fault, actors);
        var repository = new PaymentReadRepository(factory);
        batches = new(repository);
        payments = new(repository);
        reconciliation = new(repository);
        reporting = new(repository);
    }

    public Task<PaymentBatchReceiptResponseModel?> FindReplay(string tenant, string key, string hash, CancellationToken ct) => intake.FindReplay(tenant, key, hash, ct);
    public Task<PaymentBatchReceiptResponseModel> Accept(string tenant, string key, string hash, string reference, string correlation, string? traceParent, PaymentInstruction[] instructions, CancellationToken ct) => intake.Accept(tenant, key, hash, reference, correlation, traceParent, instructions, ct);
    public Task<WorkItem?> Claim(string owner, DateTime now, TimeSpan lease, CancellationToken ct) => execution.Claim(owner, now, lease, ct);

    public Task Complete(WorkItem item, BankObservation observation, DateTime now, CancellationToken ct) => execution.Complete(item, observation, now, ct);
    public Task<int> RecoverExpired(DateTime now, CancellationToken ct) => execution.RecoverExpired(now, ct);

    public Task<WorkItem> BeginRecheck(Guid caseId, int version, Guid commandId, DateTime now, CancellationToken ct) => execution.BeginRecheck(caseId, version, commandId, now, ct);
    public Task FinishRecheck(WorkItem item, BankObservation observation, DateTime now, CancellationToken ct) => execution.FinishRecheck(item, observation, now, ct);

    public Task<PaymentBatchResponseModel> Batch(Guid id, CancellationToken ct) => batches.Batch(id, ct);
    public Task<PagedResponseModel<PaymentBatchResponseModel>> Batches(int page, int size, string? status, CancellationToken ct) => batches.Batches(page, size, status, ct);

    public Task<PagedResponseModel<PaymentResponseModel>> Payments(Guid id, int page, int size, CancellationToken ct) => batches.Payments(id, page, size, ct);
    public Task<PaymentDetailResponseModel> Payment(Guid id, CancellationToken ct) => payments.Payment(id, ct);

    public Task<PagedResponseModel<ReconciliationCaseResponseModel>> Cases(int page, int size, string? status, CancellationToken ct, Guid? batchId = null) => reconciliation.Cases(page, size, status, ct, batchId);
    public Task<ReconciliationCaseDetailResponseModel> Case(Guid id, CancellationToken ct) => reconciliation.Case(id, ct);

    public Task<PaymentReportResponseModel> Report(DateTime? from, DateTime? to, string groupBy, CancellationToken ct) => reporting.Report(from, to, groupBy, ct);
    public Task<OperationsSummaryResponseModel> Summary(DateTime? from, DateTime? to, CancellationToken ct) => reporting.Summary(from, to, ct);
}
