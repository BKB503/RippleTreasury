namespace RippleTreasury.Domain;

public sealed record Money
{
    public long MinorUnits { get; }
    public string Currency { get; }

    /// <summary>Validates positive integer minor units and an explicitly supported currency.</summary>
    public Money(long minorUnits, string currency)
    {
        if (currency is not ("GBP" or "EUR" or "USD" or "JPY"))
            throw new DomainException("Unsupported currency; supported: GBP, EUR, USD, JPY.");
        if (minorUnits <= 0)
            throw new DomainException("Amount must be positive.");
        MinorUnits = minorUnits;
        Currency = currency;
    }

    /// <summary>Converts the instructed decimal amount using the supported scale, rejecting rounding and overflow.</summary>
    public static Money FromDecimal(decimal amount, string currency)
    {
        var scale = currency == "JPY" ? 1m : 100m;
        var units = checked(amount * scale);
        if (units != decimal.Truncate(units))
            throw new DomainException("Invalid currency scale.");
        return new Money(checked((long)units), currency);
    }
}
