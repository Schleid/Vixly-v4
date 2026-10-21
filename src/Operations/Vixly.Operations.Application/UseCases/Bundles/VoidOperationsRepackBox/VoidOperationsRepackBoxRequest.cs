namespace Vixly.Operations.Application.UseCases.Bundles.VoidOperationsRepackBox;

// =========================================================
// FILE: VoidOperationsRepackBoxRequest.cs
//
// PURPOSE:
// Void a repack box in no bundle, with a reason (Slice 6A, D-4): its parcels are loose again.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class VoidOperationsRepackBoxRequest
{
    public Guid BoxId { get; init; }

    public string? Reason { get; init; }
}
