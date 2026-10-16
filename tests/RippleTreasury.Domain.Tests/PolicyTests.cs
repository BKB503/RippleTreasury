using RippleTreasury.Domain;

namespace RippleTreasury.Domain.Tests;

public class PolicyTests
{
    [Fact]
    public void Given_AuthoritativeNoExecutionEvidence_When_TheRetryBudgetIsExhausted_Then_RejectFurtherClaims()
    {
        // Given
        var execution = new PaymentExecution(Guid.NewGuid(), Guid.NewGuid());

        // When
        for (var attempt = 1; attempt <= PaymentPolicy.MaxAttempts; attempt++)
        {
            execution.Claim(execution.Version);
            PaymentPolicy.Observe(execution, new(BankOutcome.ProvenNotExecuted, "Authoritative no-execution proof"), attempt);
        }

        // Then
        Assert.Equal(PaymentStatus.Rejected, execution.Status);
        Assert.Throws<DomainException>(() => execution.Claim(execution.Version));
    }

    [Fact]
    public void Given_AnAcceptedPayment_When_LateOrDuplicateEvidenceArrives_Then_PreserveAcceptance()
    {
        // Given
        var execution = new PaymentExecution(Guid.NewGuid(), Guid.NewGuid());
        execution.Claim(0);
        execution.ObserveAccepted(1, "bank-proof");

        // When
        var lateRejection = () => execution.ObserveRejected(2, "late rejected observation");
        var duplicateAcceptance = () => execution.ObserveAccepted(2, "duplicate");
        var cancellation = () => execution.Cancel(2, "cancel after bank acceptance");

        // Then
        Assert.Throws<DomainException>(lateRejection);
        Assert.Throws<DomainException>(duplicateAcceptance);
        Assert.Throws<DomainException>(cancellation);
        Assert.Equal(PaymentStatus.Accepted, execution.Status);
    }
}
