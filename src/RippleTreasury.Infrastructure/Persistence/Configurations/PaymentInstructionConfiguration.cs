using RippleTreasury.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RippleTreasury.Infrastructure;

public class PaymentInstructionConfiguration : IEntityTypeConfiguration<PaymentInstruction>
{
    public void Configure(EntityTypeBuilder<PaymentInstruction> entity)
    {
        entity.ToTable("PaymentInstructions");
        entity.OwnsOne(x => x.Treasury, TreasuryPaymentDetailsConfiguration.Configure);
        entity.Ignore(x => x.Amount);
        entity.Ignore(x => x.Parties);
        entity.Navigation(x => x.PartySnapshots).HasField("_partySnapshots").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        entity.Property(x => x.Purpose).HasColumnName("PaymentPurpose").HasConversion<string>();
        entity.Property(x => x.Provider).HasColumnName("BankProviderCode");
        entity.Property(x => x.ExecutionDate).HasColumnName("RequestedExecutionDate");
        // Exact integer storage: GBP 125.50 is 12550 minor units.
        entity.Property(x => x.AmountInMinorUnits).HasColumnName("AmountInMinorUnits");
        entity.Property(x => x.CurrencyCode).HasColumnName("CurrencyCode");
        entity.HasIndex(x => new { x.BatchId, x.ClientReference }).IsUnique();
        entity.HasOne<PaymentBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}
