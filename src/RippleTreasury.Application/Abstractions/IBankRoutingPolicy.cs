using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Selects a provider before intake commits; never called during retry or reconciliation.</summary>
public interface IBankRoutingPolicy
{
    /// <summary>Validates a preference or deterministically selects an eligible bank for the account/corridor.</summary>
    BankRoutingDecision Select(FundingAccount funding, PaymentCorridor corridor, string? preferredBank);
}
