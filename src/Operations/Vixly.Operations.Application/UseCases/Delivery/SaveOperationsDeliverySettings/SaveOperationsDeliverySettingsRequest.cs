using Vixly.Operations.Domain.Delivery;

namespace Vixly.Operations.Application.UseCases.Delivery.SaveOperationsDeliverySettings;

// =========================================================
// FILE: SaveOperationsDeliverySettingsRequest.cs
//
// PURPOSE:
// Save Settings → Delivery (Slice 7 step 3, R-12 to R-15).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsDeliverySettingsRequest
{
    public OperationsDeliveryRecording Recording { get; init; }

    public bool CashOnDelivery { get; init; }

    public int Tries { get; init; }

    public OperationsAfterFailedDelivery AfterFailed { get; init; }

    public OperationsDeliveryProofs Proofs { get; init; }

    public bool Emails { get; init; }
}
