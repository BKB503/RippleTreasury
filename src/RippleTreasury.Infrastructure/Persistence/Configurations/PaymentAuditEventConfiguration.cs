using RippleTreasury.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RippleTreasury.Infrastructure;

public class PaymentAuditEventConfiguration : IEntityTypeConfiguration<PaymentAuditEvent>
{
    public void Configure(EntityTypeBuilder<PaymentAuditEvent> entity)
    {
        entity.ToTable("PaymentAuditEvents");
        entity.HasIndex(x => new { x.PaymentId, x.At });
        entity.HasOne<PaymentExecution>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
    }
}
