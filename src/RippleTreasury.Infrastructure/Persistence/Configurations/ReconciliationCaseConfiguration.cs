using RippleTreasury.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RippleTreasury.Infrastructure;

public class ReconciliationCaseConfiguration : IEntityTypeConfiguration<ReconciliationCase>
{
    public void Configure(EntityTypeBuilder<ReconciliationCase> entity)
    {
        entity.ToTable("ReconciliationCases");
        entity.HasOne<PaymentExecution>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => x.PaymentId).IsUnique();
        entity.Property(x => x.Version).IsConcurrencyToken();
    }
}
