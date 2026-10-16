using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RippleTreasury.Domain;

namespace RippleTreasury.Infrastructure;

/// <summary>Stores the owned immutable treasury snapshot in nullable instruction columns; historical rows stay null.</summary>
internal static class TreasuryPaymentDetailsConfiguration
{
    public static void Configure(OwnedNavigationBuilder<PaymentInstruction, TreasuryPaymentDetails> owned)
    {
        owned.Property(x => x.FundingAccountId).HasColumnName("FundingAccountId");
        owned.Property(x => x.FundingCurrency).HasColumnName("FundingCurrencyCode");
        owned.Property(x => x.SourceCountry).HasColumnName("SourceCountry");
        owned.Property(x => x.FundingOwnerReference).HasColumnName("FundingOwnerReference");
        owned.Property(x => x.FundingBankProviderCode).HasColumnName("FundingBankProviderCode");
        owned.Property(x => x.BeneficiaryReference).HasColumnName("BeneficiaryReference");
        owned.Property(x => x.AccountIdentifierType).HasColumnName("BeneficiaryAccountIdentifierType");
        owned.Property(x => x.AccountIdentifier).HasColumnName("BeneficiaryAccountIdentifier");
        owned.Property(x => x.Bic).HasColumnName("BeneficiaryBic");
        owned.Property(x => x.RoutingPolicyVersion).HasColumnName("RoutingPolicyVersion");
        owned.Property(x => x.RoutingReason).HasColumnName("RoutingReason");
        owned.HasOne<FundingAccount>().WithMany().HasForeignKey(x => x.FundingAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
