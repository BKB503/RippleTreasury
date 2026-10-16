using RippleTreasury.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RippleTreasury.Infrastructure;

public class PaymentPartySnapshotConfiguration : IEntityTypeConfiguration<PaymentPartySnapshot>
{
    public void Configure(EntityTypeBuilder<PaymentPartySnapshot> entity)
    {
        entity.ToTable("PaymentPartySnapshots");
        entity.Ignore(x => x.Details);
        entity.HasOne<PaymentInstruction>().WithMany(x => x.PartySnapshots).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(x => new { x.PaymentId, x.Role }).IsUnique();
        entity.ToTable(t => t.HasCheckConstraint("CK_PartySnapshots_Role", "Role IN ('Payer', 'Payee')"));
    }
}
