namespace RippleTreasury.Domain;

/// <summary>Customer organisation owning batches; distinct from payer and payee legal companies.</summary>
public class Tenant
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public DateTime CreatedAt { get; init; }
}
