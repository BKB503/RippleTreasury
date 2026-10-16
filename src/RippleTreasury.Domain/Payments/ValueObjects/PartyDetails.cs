namespace RippleTreasury.Domain;

/// <summary>Validated party value object; no entity ID or persistence behavior. Account references are opaque tokens.</summary>
public sealed record PartyDetails
{
    public string Reference { get; }
    public string LegalName { get; }
    public string Country { get; }
    public PartyKind Kind { get; }
    public string? GroupReference { get; }
    public string AccountReference { get; }

    /// <summary>Validates immutable company/counterparty details without resolving a mutable master record.</summary>
    public PartyDetails(string reference, string legalName, string country, PartyKind kind, string? groupReference, string accountReference)
    {
        Require(reference, "Party reference");
        Require(legalName, "Party legal name");
        Require(accountReference, "Opaque account reference");
        if (!InstructionRules.SupportsCountry(country))
            throw new DomainException("Unsupported party country.");
        if (!Enum.IsDefined(kind))
            throw new DomainException("Unknown party kind.");
        if (groupReference is not null)
            Require(groupReference, "Group reference");
        Reference = reference;
        LegalName = legalName;
        Country = country;
        Kind = kind;
        GroupReference = groupReference;
        AccountReference = accountReference;
    }

    private static void Require(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100 || value.Any(char.IsControl))
            throw new DomainException($"{label} required, maximum 100 printable characters.");
    }
}
