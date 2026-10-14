namespace Vixly.Platform.Application.UseCases.Subscriptions.RecordPlatformOfflinePayment;

// =========================================================
// FILE: RecordPlatformOfflinePaymentRequest.cs
//
// PURPOSE:
// Records a payment made outside Vixly (bank transfer, cheque) and activates the subscription.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RecordPlatformOfflinePaymentRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
