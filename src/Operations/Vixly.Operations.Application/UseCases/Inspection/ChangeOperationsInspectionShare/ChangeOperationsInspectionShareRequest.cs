namespace Vixly.Operations.Application.UseCases.Inspection.ChangeOperationsInspectionShare;

// =========================================================
// FILE: ChangeOperationsInspectionShareRequest.cs
//
// PURPOSE:
// Changes how many parcels in a hundred are opened at random (Slice 3C, D-R4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsInspectionShareRequest
{
    public int Percent { get; init; }
}
