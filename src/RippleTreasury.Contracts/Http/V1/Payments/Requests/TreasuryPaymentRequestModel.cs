namespace RippleTreasury.Contracts;

/// <summary>Optional enhanced treasury intent; funding and beneficiary are mandatory when this object is supplied.</summary>
public record TreasuryPaymentRequestModel(
    string FundingAccountId,
    BeneficiaryRequestModel Beneficiary,
    string? PreferredBankId = null);
