namespace RippleTreasury.Contracts;

/// <summary>One currency total expressed as an exact minor-unit string for safe JSON consumption.</summary>
public record CurrencyTotalResponseModel(
    string Currency,
    string MinorUnits);
