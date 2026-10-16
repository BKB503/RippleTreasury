using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Integration.Tests;

internal static class TreasuryData
{
    public static CreatePaymentBatchRequestModel Batch(Fixture fixture, int count, bool mixed = false) => new("treasury", Enumerable.Range(0, count).Select(i =>
    {
        var currency = new[] { "GBP", "EUR", "USD" }[i % 3];
        var country = new[] { "GB", "DE", "US" }[i % 3];
        var bank = i % 3 == 0 ? "a" : i % 3 == 2 ? "b" : i % 2 == 0 ? "a" : "b";
        var purpose = Enum.GetValues<PaymentPurpose>()[i / 3 % 3];
        var line = PartyData.Enrich(new PaymentInstructionRequestModel($"p-{i}", 10m, currency, "Named counterparty", country,
            "", purpose.ToString(), DateOnly.FromDateTime(fixture.Clock.Now.UtcDateTime), mixed && i == 0 ? "lost-response" : mixed && i == 1 ? "reject" : "accept"));
        return line with { Treasury = new($"funding-company-uk-{currency.ToLowerInvariant()}-{bank}",
            new(line.Parties!.Payee.Reference, "TokenisedReference", line.Parties.Payee.AccountReference)) };
    }).ToArray());
}
