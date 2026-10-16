using RippleTreasury.Contracts;

namespace RippleTreasury.Application;

/// <summary>Read-only provider fixture metadata, not a bank health check.</summary>
public interface IBankCapabilityQueries
{
    /// <summary>Returns explicitly configured supported corridors and source-account IDs.</summary>
    BankCapabilityResponseModel[] List();
}
