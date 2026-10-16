using RippleTreasury.Domain;

namespace RippleTreasury.Domain.Tests;

public class ExecutionTests
{
    [Fact]
    public void Given_AnUnknownBankOutcome_When_ClaimedAgain_Then_RejectTheClaim()
    {
        // Given
        var execution = new PaymentExecution(Guid.NewGuid(), Guid.NewGuid());
        execution.Claim(0);
        execution.MarkUnknown(1, "Timeout");

        // When
        var claimAgain = () => execution.Claim(2);

        // Then
        Assert.Throws<DomainException>(claimAgain);
    }

    [Fact]
    public void Given_AnAcceptedPayment_When_ClaimedAgain_Then_PreventAnotherSubmission()
    {
        // Given
        var execution = new PaymentExecution(Guid.NewGuid(), Guid.NewGuid());
        execution.Claim(0);
        execution.MarkUnknown(1, "Timeout");
        execution.ObserveAccepted(2, "bank-evidence");

        // When
        var claimAgain = () => execution.Claim(3);

        // Then: acceptance is not settlement, but it still prevents resubmission.
        Assert.Equal(PaymentStatus.Accepted, execution.Status);
        Assert.Throws<DomainException>(claimAgain);
    }

    [Fact]
    public void Given_ANewerExecutionVersion_When_ApplyingStaleEvidence_Then_RejectTheUpdate()
    {
        // Given
        var execution = new PaymentExecution(Guid.NewGuid(), Guid.NewGuid());
        execution.Claim(0);
        execution.MarkUnknown(1, "Timeout");
        execution.ObserveAccepted(2, "bank-evidence");

        // When
        var applyStaleEvidence = () => execution.ObserveRejected(0, "stale");

        // Then
        Assert.Throws<VersionConflictException>(applyStaleEvidence);
    }

    [Fact]
    public void Given_AnUnexpiredLease_When_Recovering_Then_PreserveSubmittingState()
    {
        // Given
        var now = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
        var execution = new PaymentExecution(Guid.NewGuid(), Guid.NewGuid());
        execution.Claim(0, "worker", now, TimeSpan.FromSeconds(30));

        // When
        var recover = () => execution.RecoverExpiredLease(now.AddSeconds(10));

        // Then
        Assert.Throws<DomainException>(recover);
        Assert.Equal(PaymentStatus.Submitting, execution.Status);
        Assert.Equal("worker", execution.LeaseOwner);
    }

    [Fact]
    public void Given_ACompletedAttempt_When_RecordingDifferentEvidence_Then_RejectTheOverwrite()
    {
        // Given
        var now = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
        var attempt = new PaymentAttempt { Id = Guid.NewGuid(), StartedAt = now };
        attempt.Complete(BankOutcome.Accepted, now);

        // When
        var completeAgain = () => attempt.Complete(BankOutcome.Rejected, now.AddSeconds(1));

        // Then
        Assert.Throws<DomainException>(completeAgain);
        Assert.Equal("Accepted", attempt.Outcome);
    }
}
