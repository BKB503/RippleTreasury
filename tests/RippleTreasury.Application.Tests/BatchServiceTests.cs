using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using RippleTreasury.Application;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;
using static RippleTreasury.Application.Tests.TestSupport.LogInspection;

namespace RippleTreasury.Application.Tests;

public class BatchServiceTests
{
    [Fact]
    public async Task Given_AnInvalidPaymentLine_When_AcceptingABatch_Then_DoNotPersistInstructions()
    {
        // Given
        var store = IntakeStore();
        IBatchService service = new BatchService(store.Object);

        // When
        var error = await Assert.ThrowsAsync<InputException>(() => service.Accept(Input(0), "demo", "key", "correlation", null, default));

        // Then
        Assert.Contains("payments[0]", error.Errors.Keys);
        Assert.Empty(Hashes(store));
    }

    [Fact]
    public async Task Given_EquivalentAndChangedAmounts_When_AcceptingBatches_Then_HashFinancialIntentConsistently()
    {
        // Given
        var store = IntakeStore();
        IBatchService service = new BatchService(store.Object);

        // When
        foreach (var amount in new[] { 12.340m, 12.34m, 12.35m })
            await service.Accept(Input(amount), "demo", "key", "correlation", null, default);

        // Then
        Assert.Equal(Hashes(store)[0], Hashes(store)[1]);
        Assert.NotEqual(Hashes(store)[0], Hashes(store)[2]);
        Assert.Equal(1235, Instructions(store)[0].Amount.MinorUnits);
    }

    [Fact]
    public async Task Given_ValidIntake_When_Committed_Then_LogReceiptAndCorrelationWithoutPrivateInput()
    {
        var logger = new Mock<ILogger<BatchService>>();
        var service = new BatchService(IntakeStore().Object, logger: logger.Object);

        var result = await service.Accept(LoggingInput(), "demo", "private-key", "correlation-123", null, default);

        Assert.Contains(LogMessages(logger), x => x.Contains("committed") && x.Contains(result.Id.ToString()));
        Assert.Contains(LogScopes(logger), x => x.Contains("correlation-123"));
        Assert.DoesNotContain(LogMessages(logger), x => x.Contains("Private beneficiary") || x.Contains("private-key") || x.Contains("private-client-reference"));
    }

    [Fact]
    public async Task Given_CommitFailure_When_Accepting_Then_LogFailureWithoutClaimingSuccessOrExposingExceptionMessage()
    {
        var logger = new Mock<ILogger<BatchService>>();
        var service = new BatchService(IntakeStore(new IOException("secret connection string")).Object, logger: logger.Object);

        await Assert.ThrowsAsync<IOException>(() => service.Accept(LoggingInput(), "demo", "key", "correlation", null, default));

        Assert.Contains(LogMessages(logger), x => x.Contains("failed with IOException"));
        Assert.DoesNotContain(LogMessages(logger), x => x.Contains("committed") || x.Contains("secret connection string"));
    }

    [Fact]
    public async Task Given_InvalidPayment_When_Accepting_Then_LogValidationWithoutSuccessfulCommit()
    {
        var logger = new Mock<ILogger<BatchService>>();
        var service = new BatchService(IntakeStore().Object, logger: logger.Object);

        await Assert.ThrowsAsync<InputException>(() => service.Accept(LoggingInput(0), "demo", "key", "correlation", null, default));

        Assert.Contains(LogMessages(logger), x => x.Contains("validation failed"));
        Assert.DoesNotContain(LogMessages(logger), x => x.Contains("committed"));
    }

    [Theory]
    [InlineData("GBP", "DE", "company-uk", "mock-account-payee", "fx_not_supported")]
    [InlineData("EUR", "US", "company-uk", "mock-account-payee", "routing_unsupported")]
    [InlineData("EUR", "DE", "other-company", "mock-account-payee", "funding_owner_mismatch")]
    [InlineData("EUR", "DE", "company-uk", "12345", "beneficiary_account_unsupported")]
    public async Task Given_InvalidTreasuryIntent_When_Accepting_Then_ReturnStableCodeWithoutPersistence(
        string currency, string country, string owner, string token, string code)
    {
        // Given
        var store = IntakeStore();
        var service = Service(store);
        var input = TreasuryInput(currency, country, owner, token);
        // When
        var error = await Assert.ThrowsAsync<InputException>(() => service.Accept(input, "demo-tenant", "key", "corr", null, default));
        // Then
        Assert.Equal(code, error.ValidationCodes["payments[0]"]);
        Assert.Empty(Hashes(store));
    }

    [Fact]
    public async Task Given_EnhancedInstruction_When_Accepting_Then_PersistRoutingSnapshotAndFingerprintBeneficiaryChanges()
    {
        var store = IntakeStore();
        var service = Service(store);
        var input = TreasuryInput();
        await service.Accept(input, "demo-tenant", "key", "corr", null, default);
        var instruction = Assert.Single(Instructions(store));
        Assert.Equal("source", instruction.Treasury!.FundingAccountId);
        Assert.Equal("bank-a", instruction.Provider);
        Assert.Equal("mock-routing-v1", instruction.Treasury.RoutingPolicyVersion);
        var changedLine = input.Payments[0] with
        {
            Treasury = input.Payments[0].Treasury! with { Beneficiary = input.Payments[0].Treasury!.Beneficiary with { Bic = "DEMOUS00" } }
        };
        await service.Accept(input with { Payments = [changedLine] }, "demo-tenant", "key", "corr", null, default);
        Assert.NotEqual(Hashes(store)[0], Hashes(store)[1]);
    }

    [Fact]
    public async Task Given_LegacyWireRequest_When_Accepting_Then_RetainTheOriginalGoldenFinancialHash()
    {
        var store = IntakeStore();
        var service = new BatchService(store.Object);
        var line = new PaymentInstructionRequestModel("p-1", 12.50m, "GBP", "Vendor", "GB", "bank-a", "VendorDisbursement", new(2026, 10, 14));
        await service.Accept(new("legacy", [line]), "demo-tenant", "key", "corr", null, default);
        const string canonical = """{"ClientReference":"legacy","Payments":[{"ClientReference":"p-1","MinorUnits":1250,"Currency":"GBP","Beneficiary":"Vendor","DestinationCountry":"GB","Provider":"bank-a","Purpose":0,"ExecutionDate":"2026-10-14","Scenario":"accept"}]}""";
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))), Assert.Single(Hashes(store)));
        Assert.Null(Assert.Single(Instructions(store)).Treasury);
    }

    [Fact]
    public async Task Given_PreviouslyAcceptedTreasuryIntent_When_Replayed_Then_ReturnOriginalReceiptWithoutFundingOrRoutingChecks()
    {
        // Given
        var store = IntakeStore();
        var receipt = new PaymentBatchReceiptResponseModel(Guid.NewGuid(), true);
        store.Setup(x => x.FindReplay("demo-tenant", "key", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(receipt);
        var accounts = new Mock<IFundingAccountRepository>(MockBehavior.Strict);
        var routing = new Mock<IBankRoutingPolicy>(MockBehavior.Strict);
        var service = new BatchService(store.Object, new TreasuryInstructionFactory(accounts.Object, routing.Object));

        // When
        var result = await service.Accept(TreasuryInput(), "demo-tenant", "key", "correlation", null, default);

        // Then
        Assert.Equal(receipt, result);
        Assert.Empty(Hashes(store));
        accounts.VerifyNoOtherCalls();
        routing.VerifyNoOtherCalls();
    }

    private static CreatePaymentBatchRequestModel Input(decimal amount = 12.34m) =>
        new("batch", [new("p-1", amount, "GBP", "Vendor", "GB", "bank-a", "VendorDisbursement", new(2026, 10, 14))]);

    private static CreatePaymentBatchRequestModel LoggingInput(decimal amount = 12.34m) =>
        new("private-client-reference", [new("payment", amount, "GBP", "Private beneficiary", "GB",
            "bank-a", "VendorDisbursement", new(2026, 10, 14))]);

    private static Mock<IBatchIntakeStore> IntakeStore(Exception? error = null)
    {
        var store = new Mock<IBatchIntakeStore>(MockBehavior.Strict);
        store.Setup(x => x.FindReplay(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentBatchReceiptResponseModel?)null);
        var accept = store.Setup(x => x.Accept(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<PaymentInstruction[]>(), It.IsAny<CancellationToken>()));
        if (error is not null) accept.ThrowsAsync(error);
        else accept.ReturnsAsync((string tenant, string key, string hash, string reference, string correlation, string? trace,
            PaymentInstruction[] instructions, CancellationToken ct) => new PaymentBatchReceiptResponseModel(instructions[0].BatchId, false));
        return store;
    }

    private static string[] Hashes(Mock<IBatchIntakeStore> store) =>
        store.Invocations.Where(x => x.Method.Name == nameof(IBatchIntakeStore.Accept)).Select(x => (string)x.Arguments[2]).ToArray();

    private static PaymentInstruction[] Instructions(Mock<IBatchIntakeStore> store) =>
        (PaymentInstruction[])store.Invocations.Last(x => x.Method.Name == nameof(IBatchIntakeStore.Accept)).Arguments[6];

    private static FundingAccount Source() => new("source", "demo-tenant", "company-uk", "EUR", "GB", "bank-a", "mock-account-source");

    private static BatchService Service(Mock<IBatchIntakeStore> store) => new(store.Object,
        new(FundingRepository().Object, new BankRoutingPolicy(Capabilities(
            new BankCapability("bank-a", 1, true, ["source"], [new("DE", "EUR")])))));

    private static CreatePaymentBatchRequestModel TreasuryInput(string currency = "EUR", string country = "DE",
        string owner = "company-uk", string token = "mock-account-payee") => new("treasury", [new(
            "p-1", 12.50m, currency, "Vendor", country, "", "VendorDisbursement", new(2026, 10, 14), "accept",
            new(new(owner, "Northstar UK", "GB", "Company", "northstar", "mock-account-source"),
                new("payee", "Vendor", country, "Vendor", null, token), "INV-1"),
            new("source", new("payee", "TokenisedReference", token)))]);

    private static Mock<IFundingAccountRepository> FundingRepository()
    {
        var repository = new Mock<IFundingAccountRepository>(MockBehavior.Strict);
        repository.Setup(x => x.Find("source", "demo-tenant", It.IsAny<CancellationToken>())).ReturnsAsync(Source());
        return repository;
    }

    private static IBankCapabilityCatalog Capabilities(params BankCapability[] values)
    {
        var catalog = new Mock<IBankCapabilityCatalog>(MockBehavior.Strict);
        catalog.Setup(x => x.GetCapabilities()).Returns(values);
        return catalog.Object;
    }
}
