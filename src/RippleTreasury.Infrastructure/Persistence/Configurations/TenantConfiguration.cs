using RippleTreasury.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RippleTreasury.Infrastructure;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> entity)
    {
        entity.ToTable("Tenants");
        entity.HasKey(x => x.Id);
        entity.HasData(new Tenant { Id = "demo-tenant", Name = "Northstar Treasury Group", CreatedAt = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc) });
    }
}
