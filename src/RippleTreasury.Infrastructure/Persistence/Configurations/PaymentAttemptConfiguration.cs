using RippleTreasury.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RippleTreasury.Infrastructure;

public class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> entity)
    {
        entity.ToTable("PaymentAttempts");
        entity.HasIndex(x => new { x.PaymentId, x.StartedAt });
        entity.HasOne<PaymentExecution>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => x.CommandId).IsUnique();
    }
}
