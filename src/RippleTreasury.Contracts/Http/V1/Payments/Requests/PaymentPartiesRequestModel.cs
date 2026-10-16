namespace RippleTreasury.Contracts;

/// <summary>Requested payer, payee and invoice, intercompany transfer or loan reference.</summary>
public record PaymentPartiesRequestModel(
    PaymentPartyRequestModel Payer,
    PaymentPartyRequestModel Payee,
    string BusinessReference);
