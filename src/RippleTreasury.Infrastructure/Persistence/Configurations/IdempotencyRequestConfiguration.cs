using RippleTreasury.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RippleTreasury.Infrastructure;

public class IdempotencyRequestConfiguration : IEntityTypeConfiguration<IdempotencyRequest>
{
    public void Configure(EntityTypeBuilder<IdempotencyRequest> entity)
    {
        entity.ToTable("IdempotencyRequests");
        entity.Property(x => x.Tenant).HasColumnName("TenantId");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.Tenant).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.Tenant, x.Key }).IsUnique();
        entity.HasOne<PaymentBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
