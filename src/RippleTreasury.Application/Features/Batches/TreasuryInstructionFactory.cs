using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Resolves corporate reference data, delegates domain checks and snapshots the intake routing decision.</summary>
public class TreasuryInstructionFactory(IFundingAccountRepository accounts, IBankRoutingPolicy routing)
{
    /// <summary>Validates the tenant/company source account and beneficiary, then selects one immutable provider route.</summary>
    public async Task<TreasuryPaymentDetails> Create(PaymentInstructionRequestModel line, PaymentParties? parties,
        string tenant, CancellationToken ct)
    {
        var input = line.Treasury!;
        if (string.IsNullOrWhiteSpace(input.FundingAccountId) || input.FundingAccountId.Length > 100)
            throw new TreasuryValidationException("funding_account_required", "A funding account ID is required, maximum 100 characters.");
        var account = await accounts.Find(input.FundingAccountId, tenant, ct) ??
            throw new TreasuryValidationException("funding_account_unknown", "Funding account is not available in this tenant.");
        if (parties is null)
            throw new TreasuryValidationException("treasury_parties_required", "Enhanced treasury instructions require payer and payee snapshots.");
        account.EnsureUsable(tenant, parties.Payer.Reference, line.Currency);
        if (input.Beneficiary is null)
            throw new TreasuryValidationException("beneficiary_required", "Typed beneficiary details are required.");
        var beneficiary = new BeneficiaryAccount(input.Beneficiary.Reference, input.Beneficiary.AccountIdentifierType,
            input.Beneficiary.AccountIdentifier, input.Beneficiary.Bic);
        if (!string.IsNullOrWhiteSpace(input.PreferredBankId) && !string.IsNullOrWhiteSpace(line.Provider) && input.PreferredBankId != line.Provider)
            throw new TreasuryValidationException("routing_preference_conflict", "Provider and preferred bank must agree when both are supplied.");
        var preferred = string.IsNullOrWhiteSpace(input.PreferredBankId) ? line.Provider : input.PreferredBankId;
        var decision = routing.Select(account, new(account.Country, line.DestinationCountry, line.Currency), preferred);
        var snapshot = new TreasuryPaymentDetails(account, beneficiary, decision);
        snapshot.EnsureConsistent(Money.FromDecimal(line.Amount, line.Currency), parties);
        return snapshot;
    }
}
