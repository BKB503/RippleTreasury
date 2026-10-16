using Microsoft.Extensions.Logging;
using Moq;
using RippleTreasury.Application;
using RippleTreasury.Domain;
using static RippleTreasury.Application.Tests.TestSupport.LogInspection;

namespace RippleTreasury.Application.Tests;

public class ReconciliationServiceTests
{
    [Fact]
    public async Task Given_ABankLookupFailure_When_Rechecking_Then_RecordUnknownWithoutSubmitting()
    {
        // Given
        var store = ReconciliationStore(Work());
        var bank = Bank(lookupError: new HttpRequestException("status unavailable"));
        var processor = new ReconciliationService(store.Object, bank.Object, Clock());
        var command = Guid.NewGuid();

        // When
        await processor.Recheck(Guid.NewGuid(), 7, command, default);

        // Then
        store.Verify(x => x.BeginRecheck(It.IsAny<Guid>(), 7, command, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once());
        Assert.Equal(BankOutcome.Unknown, Reconciled(store).Outcome);
        bank.Verify(x => x.Submit(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Never());
        bank.Verify(x => x.Lookup(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Given_AStaleCaseVersion_When_Rechecking_Then_RejectBeforeCallingTheBank()
    {
        // Given
        var store = ReconciliationStore(null, new VersionConflictException());
        var bank = Bank();
        var processor = new ReconciliationService(store.Object, bank.Object, Clock());

        // When
        var recheck = () => processor.Recheck(Guid.NewGuid(), 1, Guid.NewGuid(), default);

        // Then
        await Assert.ThrowsAsync<VersionConflictException>(recheck);
        bank.Verify(x => x.Submit(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Never());
        bank.Verify(x => x.Lookup(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Given_AReconciliationCase_When_LookingUpBankStatus_Then_LogTheCaseAndCommittedOutcome()
    {
        // Given
        var store = ReconciliationStore(Work());
        var bank = Bank();
        var logger = new Mock<ILogger<ReconciliationService>>();
        var caseId = Guid.NewGuid();

        // When
        await new ReconciliationService(store.Object, bank.Object, Clock(), logger.Object).Recheck(caseId, 0, Guid.NewGuid(), default);

        // Then
        Assert.Contains(LogMessages(logger), message => message.Contains("ReconciliationObservationRecorded") && message.Contains(caseId.ToString()));
        Assert.Contains("logging-correlation", Assert.Single(LogScopes(logger)));
        bank.Verify(x => x.Submit(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Never());
        bank.Verify(x => x.Lookup(It.IsAny<WorkItem>(), It.IsAny<CancellationToken>()), Times.Once());
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

    private static Mock<IReconciliationStore> ReconciliationStore(WorkItem? item, Exception? beginError = null)
    {
        var store = new Mock<IReconciliationStore>(MockBehavior.Strict);
        var begin = store.Setup(x => x.BeginRecheck(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()));
        if (beginError is not null) begin.ThrowsAsync(beginError);
        else begin.ReturnsAsync(item!);
        store.Setup(x => x.FinishRecheck(It.IsAny<WorkItem>(), It.IsAny<BankObservation>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return store;
    }

    private static BankObservation Reconciled(Mock<IReconciliationStore> store) =>
        (BankObservation)Assert.Single(store.Invocations, x => x.Method.Name == nameof(IReconciliationStore.FinishRecheck)).Arguments[1];
}
