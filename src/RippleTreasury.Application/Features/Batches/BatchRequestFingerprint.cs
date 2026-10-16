using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Canonical client intent independent of current routing availability; preserves the established v1 JSON shape.</summary>
internal static class BatchRequestFingerprint
{
    /// <summary>Hashes normalized money/purpose/party intent plus optional treasury input, excluding generated IDs and route outcomes.</summary>
    public static string Create(CreatePaymentBatchRequestModel input)
    {
        var payments = input.Payments.Select(line =>
        {
            if (line is null) throw new DomainException("Payment is required.");
            var money = Money.FromDecimal(line.Amount, line.Currency);
            if (!Enum.TryParse<PaymentPurpose>(line.Purpose, out var purpose) || !Enum.IsDefined(purpose))
                throw new DomainException("Invalid purpose.");
            return new { line.ClientReference, money.MinorUnits, money.Currency, line.Beneficiary,
                line.DestinationCountry, Provider = line.Provider ?? "", Purpose = purpose, line.ExecutionDate, line.Scenario };
        }).ToArray();
        var canonical = JsonSerializer.Serialize(new { input.ClientReference, Payments = payments });
        if (input.Payments.Any(p => p.Parties is not null))
            canonical = JsonSerializer.Serialize(new
            {
                LegacyCanonical = canonical,
                Parties = input.Payments.Select(p => PartyInputMapper.Map(p.Parties, p.Purpose)).ToArray()
            });
        if (input.Payments.Any(p => p.Treasury is not null))
            canonical = JsonSerializer.Serialize(new { LegacyCanonical = canonical, Treasury = input.Payments.Select(p => p.Treasury).ToArray() });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
