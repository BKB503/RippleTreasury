namespace RippleTreasury.Infrastructure;

/// <summary>Optional test hook that throws immediately before commit to verify atomic rollback and crash recovery.</summary>
/// <remarks>Production does not register an implementation; it has no role in payment retry decisions.</remarks>
public interface ITransactionFaultInjector
{
    void BeforeCommit(string operation);
}
