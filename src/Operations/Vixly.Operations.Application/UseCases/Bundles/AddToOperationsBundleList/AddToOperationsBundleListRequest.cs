namespace Vixly.Operations.Application.UseCases.Bundles.AddToOperationsBundleList;

// =========================================================
// FILE: AddToOperationsBundleListRequest.cs
//
// PURPOSE:
// Put parcels and closed repack boxes on a bundle's list from the computer, to be confirmed by a scan at the station (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AddToOperationsBundleListRequest
{
    public Guid BundleId { get; init; }

    public IReadOnlyList<Guid> Ids { get; init; } = Array.Empty<Guid>();
}
