using RippleTreasury.Domain;

namespace RippleTreasury.Domain.Tests;

public class PartyRulesTests
{
    private static PartyDetails Payer(string reference = "uk", string? group = "northstar") =>
        new(reference, "Northstar UK Ltd", "GB", PartyKind.Company, group, "mock-uk");

    private static PartyDetails Payee(PartyKind kind, string? group = "northstar") =>
        new("de", "Northstar DE GmbH", "DE", kind, group, "mock-de");

    [Theory]
    [InlineData(PaymentPurpose.VendorDisbursement, PartyKind.Vendor)]
    [InlineData(PaymentPurpose.InterCompanyTransfer, PartyKind.Company)]
    [InlineData(PaymentPurpose.LoanRepayment, PartyKind.Lender)]
    public void Given_CompatiblePartyKinds_When_CreatingPaymentParties_Then_AcceptThePurpose(PaymentPurpose purpose, PartyKind kind)
    {
        // Given
        var payer = Payer();
        var payee = Payee(kind);

        // When
        var parties = new PaymentParties(payer, payee, "business-123", purpose);

        // Then
        Assert.Equal(kind, parties.Payee.Kind);
        Assert.Equal("business-123", parties.BusinessReference);
    }

    [Theory]
    [InlineData(PaymentPurpose.VendorDisbursement, PartyKind.Company)]
    [InlineData(PaymentPurpose.InterCompanyTransfer, PartyKind.Vendor)]
    [InlineData(PaymentPurpose.LoanRepayment, PartyKind.Vendor)]
    public void Given_IncompatiblePartyKinds_When_CreatingPaymentParties_Then_RejectThePurpose(PaymentPurpose purpose, PartyKind kind)
    {
        // Given
        var payer = Payer();
        var payee = Payee(kind);

        // When
        var createParties = () => new PaymentParties(payer, payee, "reference", purpose);

        // Then
        Assert.Throws<DomainException>(createParties);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("group-a", "group-b")]
    public void Given_UnrelatedOrUnknownGroups_When_CreatingIntercompanyTransfer_Then_RejectTheTransfer(string? payerGroup, string? payeeGroup)
    {
        // Given
        var payer = Payer(group: payerGroup);
        var payee = Payee(PartyKind.Company, payeeGroup);

        // When
        var createTransfer = () => new PaymentParties(payer, payee, "transfer", PaymentPurpose.InterCompanyTransfer);

        // Then
        Assert.Throws<DomainException>(createTransfer);
    }

    [Fact]
    public void Given_TheSameCompanyAsBothParties_When_CreatingIntercompanyTransfer_Then_RejectTheTransfer()
    {
        // Given
        var company = Payer();

        // When
        var createTransfer = () => new PaymentParties(company, company, "transfer", PaymentPurpose.InterCompanyTransfer);

        // Then
        Assert.Throws<DomainException>(createTransfer);
    }

    [Fact]
    public void Given_AMissingBusinessReference_When_CreatingLoanRepayment_Then_RejectTheInstruction()
    {
        // Given
        var payer = Payer();
        var lender = Payee(PartyKind.Lender);

        // When
        var createRepayment = () => new PaymentParties(payer, lender, "", PaymentPurpose.LoanRepayment);

        // Then
        Assert.Throws<DomainException>(createRepayment);
    }

    [Fact]
    public void Given_AMissingAccountReference_When_CreatingPartySnapshot_Then_RejectTheParty()
    {
        // Given
        const string accountReference = "";

        // When
        var createParty = () => new PartyDetails("party", "Vendor Ltd", "GB", PartyKind.Vendor, null, accountReference);

        // Then
        Assert.Throws<DomainException>(createParty);
    }

    [Fact]
    public void Given_AConflictingBeneficiaryName_When_CreatingInstruction_Then_RejectTheMismatch()
    {
        // Given
        var parties = new PaymentParties(Payer(), Payee(PartyKind.Vendor), "invoice", PaymentPurpose.VendorDisbursement);

        // When
        var createInstruction = () => InstructionRules.Create(Guid.NewGuid(), Guid.NewGuid(), "p", 1m, "GBP",
            "Different vendor", "DE", "bank-a", "VendorDisbursement", new(2026, 10, 14), "accept", parties);

        // Then
        Assert.Throws<DomainException>(createInstruction);
    }
}
