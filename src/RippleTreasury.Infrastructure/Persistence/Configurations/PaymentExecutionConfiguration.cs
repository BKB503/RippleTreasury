using RippleTreasury.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RippleTreasury.Infrastructure;

public class PaymentExecutionConfiguration : IEntityTypeConfiguration<PaymentExecution>
{
    public void Configure(EntityTypeBuilder<PaymentExecution> entity)
    {
        entity.ToTable("PaymentExecutions");
        entity.HasOne<PaymentInstruction>().WithOne().HasForeignKey<PaymentExecution>(x => x.Id).OnDelete(DeleteBehavior.Restrict);
        entity.Property(x => x.Version).IsConcurrencyToken();
        entity.HasIndex(x => new { x.Status, x.NextAttemptAt, x.LeaseUntil });
        entity.HasIndex(x => x.ExecutionId).IsUnique();
    }
}
