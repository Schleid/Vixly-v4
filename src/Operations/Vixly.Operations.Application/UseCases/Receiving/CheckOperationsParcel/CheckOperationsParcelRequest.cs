using Vixly.Operations.Application.Receiving;

namespace Vixly.Operations.Application.UseCases.Receiving.CheckOperationsParcel;

// =========================================================
// FILE: CheckOperationsParcelRequest.cs
//
// PURPOSE:
// Records a parcel at the inspection station: condition, weight, size and photos; picks some for compliance inspection (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CheckOperationsParcelRequest
{
    public Guid ParcelId { get; init; }

    public string Condition { get; init; } = "intact";

    public decimal Weight { get; init; }

    public decimal Length { get; init; }

    public decimal Width { get; init; }

    public decimal Height { get; init; }

    public IReadOnlyList<OperationsParcelPhotoUpload> Photos { get; init; } = Array.Empty<OperationsParcelPhotoUpload>();

    public string CommandKey { get; init; } = string.Empty;

    // The clerk sets the parcel aside for compliance inspection ("looks suspicious"), with a reason (Slice 3C).
    public bool Flag { get; init; }

    public string? FlagReason { get; init; }

    // Slice 4D step 2: the station's printer of shipping labels; a cleared parcel gets its label at once.
    public Guid? ShippingPrinterId { get; init; }
}
