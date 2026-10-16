namespace RippleTreasury.Domain;

/// <summary>Source and destination geography for a single instructed currency; no currency conversion is implied.</summary>
public sealed record PaymentCorridor(string SourceCountry, string DestinationCountry, string Currency)
{
    public bool IsCrossBorder => SourceCountry != DestinationCountry;
}
