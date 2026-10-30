using Vixly.Operations.Domain.Inspection;

namespace Vixly.Operations.Application.UseCases.Inspection.SaveOperationsRestrictedItem;

// =========================================================
// FILE: SaveOperationsRestrictedItemRequest.cs
//
// PURPOSE:
// Adds an entry to the company's restricted-items list, or changes one (Slice 3C).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsRestrictedItemRequest
{
    public Guid? ItemId { get; init; }

    public string? Name { get; init; }

    public string? Words { get; init; }

    public OperationsRestrictedItemKind Kind { get; init; } = OperationsRestrictedItemKind.Restricted;

    public string? Countries { get; init; }

    public string? Carriers { get; init; }

    public string? Requirement { get; init; }

    public bool Active { get; init; } = true;
}
