using RippleTreasury.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RippleTreasury.Infrastructure;

public class SimulatedBankExecutionConfiguration : IEntityTypeConfiguration<SimulatedBankExecution>
{
    public void Configure(EntityTypeBuilder<SimulatedBankExecution> entity)
    {
        entity.ToTable("MockBankExecutions");
        entity.Property(x => x.MinorUnits).HasColumnName("AmountInMinorUnits");
        entity.Property(x => x.Currency).HasColumnName("CurrencyCode");
    }
}
