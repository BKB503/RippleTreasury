namespace RippleTreasury.Domain;

/// <summary>A stable treasury validation code with a safe business explanation.</summary>
public class TreasuryValidationException(string code, string message) : DomainException(message)
{
    public string Code { get; } = code;
}
