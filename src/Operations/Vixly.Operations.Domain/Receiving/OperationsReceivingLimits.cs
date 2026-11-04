namespace Vixly.Operations.Domain.Receiving;

// =========================================================
// FILE: OperationsReceivingLimits.cs
//
// PURPOSE:
// Lengths and limits of receiving (Slice 3B).
//
// RESPONSIBILITIES:
// - Keep columns and checks in step
//
// IMPORTANT NOTES:
// - Weights in kilograms, sizes in centimetres (D-O8)
// =========================================================
public static class OperationsReceivingLimits
{
    public const int CarrierMaxLength = 40;

    public const int TrackingNumberMaxLength = 40;

    public const int StoreMaxLength = 100;

    public const int ContentsMaxLength = 300;

    public const int LocationCodeMaxLength = 20;

    public const int NumberMaxLength = 20;

    public const int StorageKeyMaxLength = 200;

    public const decimal MaxWeightKg = 1000m;

    public const decimal MaxSideCm = 400m;

    public const decimal MaxDeclaredValue = 1_000_000m;

    public const int MaxPhotoBytes = 10 * 1024 * 1024;

    public const int MaxPhotosPerReceipt = 6;
}
