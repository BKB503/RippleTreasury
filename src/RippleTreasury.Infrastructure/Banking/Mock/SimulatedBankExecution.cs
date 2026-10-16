namespace RippleTreasury.Infrastructure;

/// <summary>Bank-side simulation record used to detect replayed execution keys and verify a single financial effect.</summary>
/// <remarks>Not a Treasury payment aggregate or a real bank transfer. EF retains the physical MockBankExecutions table.</remarks>
public class SimulatedBankExecution
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = "";
    public string Outcome { get; set; } = "";
    public string Evidence { get; set; } = "";
    public int FinancialExecutions { get; set; }
    public string InstructionHash { get; set; } = "";
    public long MinorUnits { get; set; }
    public string Currency { get; set; } = "";
}
