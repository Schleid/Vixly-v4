using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.Locations.SaveOperationsBranch;

// =========================================================
// FILE: SaveOperationsBranchRequest.cs
//
// PURPOSE:
// Creates or changes a branch (Slice 1B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsBranchRequest
{
    public Guid? BranchId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Line1 { get; init; } = string.Empty;

    public string? Line2 { get; init; }

    public string City { get; init; } = string.Empty;

    public string? Region { get; init; }

    public string? PostalCode { get; init; }

    public string CountryCode { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? OpeningHours { get; init; }

    public bool ShowOnSite { get; init; }

    public OperationsLocationStatus Status { get; init; }

    public OperationsBranchSide Side { get; init; } = OperationsBranchSide.Both;
}
