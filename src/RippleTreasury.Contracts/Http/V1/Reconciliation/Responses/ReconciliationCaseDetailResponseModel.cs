namespace RippleTreasury.Contracts;

/// <summary>Reconciliation case and consistent payment evidence used by the operations drawer.</summary>
public record ReconciliationCaseDetailResponseModel(
    ReconciliationCaseResponseModel Case,
    PaymentDetailResponseModel Detail);
