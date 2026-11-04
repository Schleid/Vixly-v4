namespace Vixly.Operations.Domain.Bundles;

// =========================================================
// FILE: OperationsBundleLimits.cs
//
// PURPOSE:
// The limits of bundles and repack boxes (Slice 6A).
//
// RESPONSIBILITIES:
// - Hold the numbers the domain, the database and the forms share
//
// IMPORTANT NOTES:
// - Weights in kilograms and sizes in centimetres (D-O8); a pallet or a container is bigger than a parcel
// =========================================================
public static class OperationsBundleLimits
{
    public const int TypeMaxLength = 30;

    public const int MaxTypes = 12;

    public const int ReasonMaxLength = 300;

    public const int MaxParcels = 2000;

    public const decimal MaxWeightKg = 30_000m;

    public const decimal MaxSideCm = 1_500m;
}
