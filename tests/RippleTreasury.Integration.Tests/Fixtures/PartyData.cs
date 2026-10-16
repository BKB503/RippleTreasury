using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Integration.Tests;

internal static class PartyData
{

    public static PaymentInstructionRequestModel Enrich(PaymentInstructionRequestModel payment)
    {
        var kind = Enum.Parse<PaymentPurpose>(payment.Purpose) switch
        {
            PaymentPurpose.InterCompanyTransfer => "Company",
            PaymentPurpose.LoanRepayment => "Lender",
            _ => "Vendor"
        };
        var payer = new PaymentPartyRequestModel("company-uk", "Northstar Treasury UK Ltd", "GB", "Company", "northstar-group", "mock-account-uk");
        var payee = new PaymentPartyRequestModel("payee-" + payment.ClientReference, payment.Beneficiary, payment.DestinationCountry,
            kind, kind == "Company" ? "northstar-group" : null, "mock-account-payee");
        return payment with
        {
            Parties = new(payer, payee, payment.Purpose + "-" + payment.ClientReference)
        };
    }
}
