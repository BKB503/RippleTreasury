using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using RippleTreasury.Application;
using RippleTreasury.Domain;
using static RippleTreasury.Application.Tests.TestSupport.LogInspection;

namespace RippleTreasury.Application.Tests;

public class DispatchServiceTests
{
    [Fact]
    public async Task Given_NoEligiblePayment_When_Dispatching_Then_DoNotCallTheBank()
    {
        // Given
        var store = ExecutionStore();
        var bank = Bank();
        var processor = new DispatchService(store.Object, bank.Object, Clock());

        // When
        var dispatched = await processor.DispatchOne("test-worker", default);

        // Then
        Assert.False(dispatched);
        bank.Verify(x => x.Submit(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Never());
        store.Verify(x => x.Complete(It.IsAny<WorkItem>(), It.IsAny<BankObservation>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Given_AnAmbiguousTransportFailure_When_Dispatching_Then_RecordUnknownWithoutResubmitting(bool timeout)
    {
        // Given
        var store = ExecutionStore(Work());
        var bank = Bank(submitError: timeout ? new TimeoutException("response lost") : new HttpRequestException("disconnected"));
        var processor = new DispatchService(store.Object, bank.Object, Clock());

        // When
        var dispatched = await processor.DispatchOne("test-worker", default);

        // Then
        Assert.True(dispatched);
        Assert.Equal(BankOutcome.Unknown, Completed(store).Outcome);
        bank.Verify(x => x.Submit(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Once());
        bank.Verify(x => x.Lookup(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Never());
        var secondDispatch = await processor.DispatchOne("test-worker", default);
        Assert.False(secondDispatch);
    }

    [Fact]
    public async Task Given_AResultCommitFailure_When_Dispatching_Then_PropagateFailureWithoutAnotherBankSubmission()
    {
        // Given
        var store = ExecutionStore(Work(), new IOException("result commit unavailable"));
        var bank = Bank();
        var processor = new DispatchService(store.Object, bank.Object, Clock());

        // When
        var dispatch = () => processor.DispatchOne("test-worker", default);

        // Then
        await Assert.ThrowsAsync<IOException>(dispatch);
        bank.Verify(x => x.Submit(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Once());
        store.Verify(x => x.Complete(It.IsAny<WorkItem>(), It.IsAny<BankObservation>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once());
        Assert.Equal(1, await processor.Recover(default));
        store.Verify(x => x.RecoverExpired(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once());
        bank.Verify(x => x.Submit(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Given_CancellationBeforeClaim_When_Dispatching_Then_DoNotSubmitToTheBank()
    {
        // Given
        var store = ExecutionStore(Work());
        var bank = Bank();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        store.Setup(x => x.Claim(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        // When
        var dispatch = () => new DispatchService(store.Object, bank.Object, Clock()).DispatchOne("test-worker", cancellation.Token);

        // Then
        await Assert.ThrowsAsync<OperationCanceledException>(dispatch);
        bank.Verify(x => x.Submit(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Given_PersistedTraceContext_When_Dispatching_Then_PreserveTraceAndCorrelation()
    {
        // Given
        const string parent = "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01";
        Activity? captured = null;
        using var listener = new ActivityListener { ShouldListenTo = source => source.Name == Telemetry.Name, Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded, ActivityStopped = activity => { if (activity.TraceId.ToString() == "0123456789abcdef0123456789abcdef") captured = activity; } };
        ActivitySource.AddActivityListener(listener);
        var store = ExecutionStore(Work(parent));

        // When
        await new DispatchService(store.Object, Bank().Object, Clock()).DispatchOne("test-worker", default);

        // Then
        Assert.NotNull(captured);
        Assert.Equal("0123456789abcdef0123456789abcdef", captured.TraceId.ToString());
        Assert.Equal("logging-correlation", captured.GetTagItem("correlation.id"));
    }

    [Fact]
    public async Task Given_AFailedResultCommit_When_Dispatching_Then_LogFailureWithoutClaimingSuccess()
    {
        // Given
        var item = Work();
        var store = ExecutionStore(item, new IOException("private details"));
        var logger = new Mock<ILogger<DispatchService>>();

        // When
        var dispatch = () => new DispatchService(store.Object, Bank().Object, Clock(), logger.Object).DispatchOne("worker", default);

        // Then
        await Assert.ThrowsAsync<IOException>(dispatch);
        Assert.Contains(LogMessages(logger), message => message.StartsWith("PaymentOutcomeCommitFailed"));
        Assert.DoesNotContain(LogMessages(logger), message => message.StartsWith("PaymentOutcomeRecorded"));
        Assert.Contains(item.Execution.ExecutionId.ToString(), Assert.Single(LogScopes(logger)));
        Assert.DoesNotContain("PRIVATE BENEFICIARY", string.Join(" ", LogMessages(logger).Concat(LogScopes(logger))));
        Assert.DoesNotContain("private details", string.Join(" ", LogMessages(logger)));
    }

    [Fact]
    public async Task Given_AmbiguousTransportWithSensitiveDetails_When_Dispatching_Then_LogUnknownWithoutPrivateData()
    {
        // Given
        var store = ExecutionStore(Work());
        var bank = Bank(submitError: new HttpRequestException("PRIVATE ACCOUNT 123456"));
        var logger = new Mock<ILogger<DispatchService>>();

        // When
        await new DispatchService(store.Object, bank.Object, Clock(), logger.Object).DispatchOne("worker", default);

        // Then
        Assert.Contains(LogMessages(logger), message => message.Contains("SubmissionTransportAmbiguous FailureType=HttpRequestException"));
        Assert.Contains(LogMessages(logger), message => message.Contains("PaymentOutcomeRecorded Outcome=Unknown"));
        Assert.DoesNotContain("PRIVATE ACCOUNT", string.Join(" ", LogMessages(logger).Concat(LogScopes(logger))));
        bank.Verify(x => x.Submit(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    private static WorkItem Work(string? trace = null)
    {
        var id = Guid.NewGuid();
        var execution = new PaymentExecution(id, Guid.NewGuid());
        execution.Claim(0);
        return new(new(id, Guid.NewGuid(), "p-1", new Money(1234, "GBP"), "PRIVATE BENEFICIARY", "GB", "bank-a", PaymentPurpose.VendorDisbursement, new(2026, 10, 14), "accept"), execution, Guid.NewGuid(), "test-worker", "logging-correlation", trace, 1);
    }

    private static TimeProvider Clock() =>
        Mock.Of<TimeProvider>(x => x.GetUtcNow() == new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero));

    private static Mock<IBankGateway> Bank(Exception? submitError = null, Exception? lookupError = null)
    {
        var bank = new Mock<IBankGateway>(MockBehavior.Strict);
        var submit = bank.Setup(x => x.Submit(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()));
        if (submitError is not null) submit.ThrowsAsync(submitError);
        else submit.ReturnsAsync(new BankObservation(BankOutcome.Accepted, "bank-proof"));
        var lookup = bank.Setup(x => x.Lookup(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()));
        if (lookupError is not null) lookup.ThrowsAsync(lookupError);
        else lookup.ReturnsAsync(new BankObservation(BankOutcome.Accepted, "bank-proof"));
        return bank;
    }

    private static Mock<IExecutionStore> ExecutionStore(WorkItem? item = null, Exception? commitError = null)
    {
        var store = new Mock<IExecutionStore>(MockBehavior.Strict);
        store.SetupSequence(x => x.Claim(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(item)
            .ReturnsAsync((WorkItem?)null);
        var complete = store.Setup(x => x.Complete(It.IsAny<WorkItem>(), It.IsAny<BankObservation>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()));
        if (commitError is not null) complete.ThrowsAsync(commitError);
        else complete.Returns(Task.CompletedTask);
        store.Setup(x => x.RecoverExpired(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return store;
    }

    private static BankObservation Completed(Mock<IExecutionStore> store) =>
        (BankObservation)Assert.Single(store.Invocations, x => x.Method.Name == nameof(IExecutionStore.Complete)).Arguments[1];
}
