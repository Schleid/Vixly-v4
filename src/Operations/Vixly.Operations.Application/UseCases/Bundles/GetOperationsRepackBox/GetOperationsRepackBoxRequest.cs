namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBox;

// =========================================================
// FILE: GetOperationsRepackBoxRequest.cs
//
// PURPOSE:
// One repack box (Slice 6A, D-4): its shipment's parcels, in the box or still to add, with their shelves.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsRepackBoxRequest
{
    public Guid BoxId { get; init; }
}
