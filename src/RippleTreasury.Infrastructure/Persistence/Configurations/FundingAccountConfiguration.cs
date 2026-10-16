using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

/// <summary>Maps corporate account reference data separately from immutable accepted instruction snapshots.</summary>
public class FundingAccountConfiguration : IEntityTypeConfiguration<FundingAccount>
{
    public void Configure(EntityTypeBuilder<FundingAccount> entity)
    {
        entity.ToTable("FundingAccounts");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.TenantId, x.OwnerReference, x.CurrencyCode });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasData(DemoFundingAccounts.Create());
    }
}
