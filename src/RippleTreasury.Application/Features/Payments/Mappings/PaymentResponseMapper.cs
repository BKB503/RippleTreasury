using System.Globalization;
using RippleTreasury.Contracts;
using RippleTreasury.Domain;

namespace RippleTreasury.Application;

/// <summary>Maps one payment and its accepted party snapshots to read-only response models.</summary>
internal static class PaymentResponseMapper
{
    /// <summary>Combines accepted intent and current execution state into one payment response.</summary>
    public static PaymentResponseModel Map(PaymentBatch batch, PaymentInstruction instruction, PaymentExecution execution) =>
        new(instruction.Id, instruction.BatchId, execution.ExecutionId, instruction.ClientReference,
            instruction.AmountInMinorUnits.ToString(CultureInfo.InvariantCulture), instruction.CurrencyCode,
            instruction.Beneficiary, instruction.DestinationCountry, instruction.Provider, instruction.Purpose.ToString(),
            execution.Status.ToString(), execution.Version, execution.BankReference, batch.CorrelationId,
            MapParties(instruction.Parties), instruction.ExecutionDate, TreasuryPaymentResponseMapper.Map(instruction));

    /// <summary>Maps accepted payer/payee snapshots to response models, preserving absent legacy party data.</summary>
    private static PaymentPartiesResponseModel? MapParties(PaymentParties? parties) => parties is null ? null :
        new(MapParty(parties.Payer), MapParty(parties.Payee), parties.BusinessReference);

    /// <summary>Copies accepted party details to the response without changing financial or account data.</summary>
    private static PaymentPartyResponseModel MapParty(PartyDetails party) => new(party.Reference, party.LegalName,
        party.Country, party.Kind.ToString(), party.GroupReference, party.AccountReference);
}
