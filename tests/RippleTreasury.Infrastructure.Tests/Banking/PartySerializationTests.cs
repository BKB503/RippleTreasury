using System.Text.Json;
using RippleTreasury.Domain;
using RippleTreasury.Infrastructure;

namespace RippleTreasury.Infrastructure.Tests;

public class PartySerializationTests
{
    [Fact]
    public void Given_ValidatedPartySnapshots_When_SerializingTheBankPayload_Then_PreserveIntentAndFingerprint()
    {
        // Given
        var parties = new PaymentParties(new("uk", "Northstar UK Ltd", "GB", PartyKind.Company, "group", "mock-uk"),
            new("de", "Northstar Germany GmbH", "DE", PartyKind.Company, "group", "mock-de"), "transfer-1", PaymentPurpose.InterCompanyTransfer);
        var payload = new MockSubmission(Guid.NewGuid(), "accept", 1, 1234, "EUR", parties.Payee.LegalName, "DE", "InterCompanyTransfer", new(2026, 10, 14), parties);

        // When
        var json = JsonSerializer.Serialize(payload, JsonSerializerOptions.Web);
        var restored = JsonSerializer.Deserialize<MockSubmission>(json, JsonSerializerOptions.Web)!;

        // Then
        Assert.Equal(payload, restored);
        Assert.Equal(payload.Fingerprint(), restored.Fingerprint());
        var different = new PaymentParties(parties.Payer, new("de", "Renamed subsidiary", "DE", PartyKind.Company, "group", "mock-de"), "transfer-1", PaymentPurpose.InterCompanyTransfer);
        Assert.NotEqual(payload.Fingerprint(), (payload with
        {
            Parties = different
        }).Fingerprint());
    }
}
