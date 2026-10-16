using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Translates request party models into domain value objects and delegates financial validation to Domain.</summary>
internal static class PartyInputMapper
{

    /// <summary>Converts optional request parties into validated domain payment parties for the supplied purpose.</summary>
    public static PaymentParties? Map(PaymentPartiesRequestModel? input, string purpose)
    {
        // Optional solely for compatibility with the original mock-only v1 request.
        // New demos provide explicit snapshots; production admission requires them.
        if (input is null)
            return null;
        if (!Enum.TryParse<PaymentPurpose>(purpose, out var parsed) || !Enum.IsDefined(parsed))
            throw new DomainException("Invalid purpose.");
        return new(MapParty(input.Payer), MapParty(input.Payee), input.BusinessReference, parsed);
    }

    /// <summary>Validates the supplied party kind and constructs the immutable party-details value object.</summary>
    private static PartyDetails MapParty(PaymentPartyRequestModel input)
    {
        if (input is null)
            throw new DomainException("Payer and payee required.");
        if (!Enum.TryParse<PartyKind>(input.Kind, out var kind) || !Enum.IsDefined(kind))
            throw new DomainException("Invalid party kind.");
        return new(input.Reference, input.LegalName, input.Country, kind, input.GroupReference, input.AccountReference);
    }
}
