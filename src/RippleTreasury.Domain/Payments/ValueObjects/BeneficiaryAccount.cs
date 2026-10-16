using System.Text.RegularExpressions;

namespace RippleTreasury.Domain;

/// <summary>Validated fictional beneficiary identity and token. Real IBAN/domestic-account validation is not implemented.</summary>
public sealed record BeneficiaryAccount
{
    /// <summary>Validates a fictional token and optional BIC syntax without claiming real account verification.</summary>
    public BeneficiaryAccount(string reference, string identifierType, string identifier, string? bic = null)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 100)
            throw new TreasuryValidationException("beneficiary_reference_invalid", "Beneficiary reference is required, maximum 100 characters.");
        if (identifierType != "TokenisedReference" || identifier is null ||
            !Regex.IsMatch(identifier, @"\Amock-account-[A-Za-z0-9-]{1,64}\z", RegexOptions.CultureInvariant))
            throw new TreasuryValidationException("beneficiary_account_unsupported", "Mock banks support TokenisedReference identifiers beginning mock-account- only.");
        if (bic is not null && !Regex.IsMatch(bic, @"\A[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?\z", RegexOptions.CultureInvariant))
            throw new TreasuryValidationException("beneficiary_bic_invalid", "BIC must have the supported 8- or 11-character format.");
        Reference = reference; IdentifierType = identifierType; Identifier = identifier; Bic = bic;
    }

    public string Reference { get; }
    public string IdentifierType { get; }
    public string Identifier { get; }
    public string? Bic { get; }
}
