namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsItemType;

// =========================================================
// FILE: SaveOperationsItemTypeRequest.cs
//
// PURPOSE:
// Adds one of the company's item types or changes one: a barrel, a car, a TV (Slice 4B design section 7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsItemTypeRequest
{
    public Guid? ItemTypeId { get; init; }

    public string? Name { get; init; }

    public string? Words { get; init; }

    public bool Active { get; init; } = true;
}
