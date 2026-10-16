namespace RippleTreasury.Contracts;

/// <summary>Available corporate funding metadata, not account balances or raw account identifiers.</summary>
public record FundingAccountResponseModel(
    string Id,
    string OwnerReference,
    string Currency,
    string Country,
    string Provider);
