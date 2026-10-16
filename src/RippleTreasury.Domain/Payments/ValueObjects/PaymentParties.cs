namespace RippleTreasury.Domain;

/// <summary>Purpose-specific payer, payee and obligation captured with the accepted instruction.</summary>
public sealed record PaymentParties
{
    public PartyDetails Payer { get; }
    public PartyDetails Payee { get; }
    public string BusinessReference { get; }
    public PaymentPurpose Purpose { get; }

    /// <summary>Validates payer/payee kind, corporate-group relationships and the business reference for the payment purpose.</summary>
    public PaymentParties(PartyDetails payer, PartyDetails payee, string businessReference, PaymentPurpose purpose)
    {
        if (payer is null || payee is null)
            throw new DomainException("Payer and payee required.");
        if (string.IsNullOrWhiteSpace(businessReference) || businessReference.Length > 100 || businessReference.Any(char.IsControl))
            throw new DomainException("Invoice, intercompany transfer or loan reference required, maximum 100 printable characters.");
        if (payer.Kind != PartyKind.Company)
            throw new DomainException("Payer must be a company.");
        var expected = purpose switch
        {
            PaymentPurpose.VendorDisbursement => PartyKind.Vendor,
            PaymentPurpose.InterCompanyTransfer => PartyKind.Company,
            PaymentPurpose.LoanRepayment => PartyKind.Lender,
            _ => throw new DomainException("Unknown payment purpose.")
        };
        if (payee.Kind != expected)
            throw new DomainException($"{purpose} requires a {expected} payee.");
        if (payer.Reference == payee.Reference)
            throw new DomainException("Payer and payee must be distinct parties.");
        if (purpose == PaymentPurpose.InterCompanyTransfer &&
            (string.IsNullOrWhiteSpace(payer.GroupReference) || payer.GroupReference != payee.GroupReference))
            throw new DomainException("Intercompany parties must belong to the same corporate group.");
        Payer = payer;
        Payee = payee;
        BusinessReference = businessReference;
        Purpose = purpose;
    }
}
