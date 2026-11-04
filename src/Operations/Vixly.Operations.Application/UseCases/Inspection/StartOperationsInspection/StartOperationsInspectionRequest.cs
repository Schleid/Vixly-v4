namespace Vixly.Operations.Application.UseCases.Inspection.StartOperationsInspection;

// =========================================================
// FILE: StartOperationsInspectionRequest.cs
//
// PURPOSE:
// The inspector opens the box: the inspection is in progress (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StartOperationsInspectionRequest
{
    public Guid InspectionId { get; init; }

    public string CommandKey { get; init; } = string.Empty;
}
