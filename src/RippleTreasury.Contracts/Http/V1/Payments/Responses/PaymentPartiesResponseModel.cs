namespace RippleTreasury.Contracts;

/// <summary>Accepted company/counterparty snapshots and business purpose reference.</summary>
public record PaymentPartiesResponseModel(
    PaymentPartyResponseModel Payer,
    PaymentPartyResponseModel Payee,
    string BusinessReference);
