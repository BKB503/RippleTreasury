using RippleTreasury.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RippleTreasury.Infrastructure;

public class PaymentBatchConfiguration : IEntityTypeConfiguration<PaymentBatch>
{
    public void Configure(EntityTypeBuilder<PaymentBatch> entity)
    {
        entity.ToTable("PaymentBatches");
        entity.Property(x => x.Tenant).HasColumnName("TenantId");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.Tenant).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.Tenant, x.ClientReference }).IsUnique();
    }
}
