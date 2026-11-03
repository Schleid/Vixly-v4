using Vixly.Operations.Domain.Payables;

namespace Vixly.Operations.Application.UseCases.Payables.SaveOperationsVendor;

// =========================================================
// FILE: SaveOperationsVendorRequest.cs
//
// PURPOSE:
// Adds or changes a vendor (Slice 8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsVendorRequest
{
    public Guid? VendorId { get; init; }

    public string? Name { get; init; }

    public OperationsBillCategory Category { get; init; }

    public string? Contact { get; init; }

    public bool Active { get; init; } = true;
}
