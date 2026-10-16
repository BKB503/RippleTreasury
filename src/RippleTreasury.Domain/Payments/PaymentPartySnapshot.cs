namespace RippleTreasury.Domain;

/// <summary>An identified payer/payee snapshot belonging to an immutable accepted instruction.</summary>
public class PaymentPartySnapshot
{
    public Guid Id { get; init; }
    public Guid PaymentId { get; init; }
    public string Role { get; init; } = "";
    public string Reference { get; init; } = "";
    public string LegalName { get; init; } = "";
    public string Country { get; init; } = "";
    public string Kind { get; init; } = "";
    public string? GroupReference { get; init; }
    public string AccountReference { get; init; } = "";

    public PartyDetails Details => new(Reference, LegalName, Country, Enum.Parse<PartyKind>(Kind), GroupReference, AccountReference);

    /// <summary>Captures one identified immutable payer/payee snapshot for an accepted instruction.</summary>
    public static PaymentPartySnapshot Capture(Guid paymentId, string role, PartyDetails details) => new()
    {
        Id = Guid.NewGuid(), PaymentId = paymentId, Role = role, Reference = details.Reference,
        LegalName = details.LegalName, Country = details.Country, Kind = details.Kind.ToString(),
        GroupReference = details.GroupReference, AccountReference = details.AccountReference
    };
}
