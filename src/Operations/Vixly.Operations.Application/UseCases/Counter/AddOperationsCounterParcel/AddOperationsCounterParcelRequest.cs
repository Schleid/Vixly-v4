namespace Vixly.Operations.Application.UseCases.Counter.AddOperationsCounterParcel;

// =========================================================
// FILE: AddOperationsCounterParcelRequest.cs
//
// PURPOSE:
// Enters a parcel at the counter: weighed, measured and described in front of the customer; the last one makes the shipment final (Slice 5C, C-2 and C-7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AddOperationsCounterParcelRequest
{
    public Guid ShipmentId { get; init; }

    public decimal? Weight { get; init; }

    public decimal? Length { get; init; }

    public decimal? Width { get; init; }

    public decimal? Height { get; init; }

    public string? Contents { get; init; }

    public decimal? Value { get; init; }

    public string? CommandKey { get; init; }
}
