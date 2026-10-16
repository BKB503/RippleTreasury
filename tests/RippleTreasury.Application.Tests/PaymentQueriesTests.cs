using Moq;
using RippleTreasury.Application;

namespace RippleTreasury.Application.Tests;

public class PaymentQueriesTests
{
    [Fact]
    public async Task Given_MissingPayment_When_LoadingEvidence_Then_ForwardIdentityAndCancellationAndPreserveNotFound()
    {
        // Given
        var id = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var repository = new Mock<IPaymentReadRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetPayment(id, cancellation.Token)).ThrowsAsync(new MissingException());

        // When
        var load = () => new PaymentQueries(repository.Object).Payment(id, cancellation.Token);

        // Then
        await Assert.ThrowsAsync<MissingException>(load);
        repository.Verify(x => x.GetPayment(id, cancellation.Token), Times.Once());
        repository.VerifyNoOtherCalls();
    }
}
