namespace Vixly.Operations.Domain.Bundles;

// =========================================================
// FILE: OperationsBundleDraft.cs
//
// PURPOSE:
// What the ship manager chooses for a new bundle (Slice 6A, D-2).
//
// RESPONSIBILITIES:
// - Carry the destination, the service, the type and the size policy
//
// IMPORTANT NOTES:
// - Checked by OperationsBundle.Create; the target is in kilograms (actual or volumetric, as the basis)
// =========================================================
public sealed record OperationsBundleDraft(
    Guid WarehouseId,
    Guid? DestinationWarehouseId,
    string? CountryCode,
    string? ServiceCode,
    string? TypeName,
    OperationsBundleBasis Basis,
    decimal? TargetKg,
    decimal? Divisor);
