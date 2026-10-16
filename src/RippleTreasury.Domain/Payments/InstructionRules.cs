namespace RippleTreasury.Domain;

public static class InstructionRules
{

    /// <summary>Checks the explicitly supported demo country codes, not every possible international corridor.</summary>
    public static bool SupportsCountry(string country) => country is "GB" or "DE" or "FR" or "US" or "JP" or "IE" or "NL";
    /// <summary>Validates immutable instruction intent and builds the domain entity without dispatching money.</summary>
    public static PaymentInstruction Create(Guid id, Guid batchId, string reference, decimal amount, string currency, string beneficiary, string country, string provider, string purpose, DateOnly date, string scenario, PaymentParties? parties = null, TreasuryPaymentDetails? treasury = null)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 100)
            throw new DomainException("Payment reference required, maximum 100 characters.");
        if (string.IsNullOrWhiteSpace(beneficiary) || beneficiary.Length > 100)
            throw new DomainException("Mock beneficiary required, maximum 100 characters.");
        if (!SupportsCountry(country))
            throw new DomainException("Unsupported destination country.");
        if (provider is not ("bank-a" or "bank-b"))
            throw new DomainException("Unknown provider.");
        if (!Enum.TryParse<PaymentPurpose>(purpose, out var parsedPurpose) || !Enum.IsDefined(parsedPurpose))
            throw new DomainException("Invalid purpose.");
        if (scenario is not ("accept" or "reject" or "lost-response" or "unknown" or "safe-transient" or "rate-limit"))
            throw new DomainException("Invalid mock scenario.");
        if (scenario == "rate-limit" && provider != "bank-b")
            throw new TreasuryValidationException("mock_scenario_unsupported", "The rate-limit fixture belongs to bank-b only.");
        if (date == default)
            throw new DomainException("Execution date required.");
        EnsurePartyConsistency(parties, parsedPurpose, beneficiary, country);
        return new(id, batchId, reference, Money.FromDecimal(amount, currency), beneficiary, country, provider, parsedPurpose, date, scenario, parties, treasury);
    }

    /// <summary>Requires accepted payee name/country and purpose to agree with the party snapshots.</summary>
    public static void EnsurePartyConsistency(PaymentParties? parties, PaymentPurpose purpose, string beneficiary, string country)
    {
        if (parties is not null)
        {
            _ = new PaymentParties(parties.Payer, parties.Payee, parties.BusinessReference, purpose);
            if (parties.Payee.Country != country || parties.Payee.LegalName != beneficiary)
                throw new DomainException("Beneficiary and destination must match the payee snapshot.");
        }
    }
}
