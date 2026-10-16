namespace RippleTreasury.Domain;

/// <summary>Accepted financial intent. Amount and counterparties cannot be edited after acceptance.</summary>
public class PaymentInstruction
{
    private readonly List<PaymentPartySnapshot> _partySnapshots = [];

    // Materialization uses private setters; no EF or database dependencies enter Domain.
    /// <summary>Creates immutable accepted intent and snapshots validated party/treasury details; the private constructor restores stored data.</summary>
    private PaymentInstruction() { }

    /// <summary>Creates immutable accepted intent and snapshots validated party/treasury details; the private constructor restores stored data.</summary>
    public PaymentInstruction(Guid id, Guid batchId, string clientReference, Money amount, string beneficiary,
        string destinationCountry, string provider, PaymentPurpose purpose, DateOnly executionDate, string scenario,
        PaymentParties? parties = null, TreasuryPaymentDetails? treasury = null)
    {
        InstructionRules.EnsurePartyConsistency(parties, purpose, beneficiary, destinationCountry);
        treasury?.EnsureConsistent(amount, parties);
        if (treasury is not null && treasury.FundingBankProviderCode != provider)
            throw new TreasuryValidationException("funding_provider_mismatch", "Instruction provider must match the accepted funding route.");
        Treasury = treasury;
        Id = id;
        BatchId = batchId;
        ClientReference = clientReference;
        AmountInMinorUnits = amount.MinorUnits;
        CurrencyCode = amount.Currency;
        Beneficiary = beneficiary;
        DestinationCountry = destinationCountry;
        Provider = provider;
        Purpose = purpose;
        ExecutionDate = executionDate;
        Scenario = scenario;
        BusinessReference = parties?.BusinessReference;
        if (parties is not null)
        {
            _partySnapshots.Add(PaymentPartySnapshot.Capture(id, "Payer", parties.Payer));
            _partySnapshots.Add(PaymentPartySnapshot.Capture(id, "Payee", parties.Payee));
        }
    }

    public TreasuryPaymentDetails? Treasury { get; private set; }
    public Guid Id { get; private set; }
    public Guid BatchId { get; private set; }
    public string ClientReference { get; private set; } = "";
    public long AmountInMinorUnits { get; private set; }
    public string CurrencyCode { get; private set; } = "";
    public Money Amount => new(AmountInMinorUnits, CurrencyCode);
    public string Beneficiary { get; private set; } = "";
    public string DestinationCountry { get; private set; } = "";
    public string Provider { get; private set; } = "";
    public PaymentPurpose Purpose { get; private set; }
    public DateOnly ExecutionDate { get; private set; }
    public string Scenario { get; private set; } = "";
    public string? BusinessReference { get; private set; }
    public IReadOnlyCollection<PaymentPartySnapshot> PartySnapshots => _partySnapshots.AsReadOnly();

    public PaymentParties? Parties
    {
        get
        {
            if (_partySnapshots.Count == 0 && BusinessReference is null) return null;
            if (_partySnapshots.Count != 2) throw new DomainException("Incomplete immutable party snapshot.");
            return new(_partySnapshots.Single(x => x.Role == "Payer").Details,
                _partySnapshots.Single(x => x.Role == "Payee").Details, BusinessReference!, Purpose);
        }
    }
}
