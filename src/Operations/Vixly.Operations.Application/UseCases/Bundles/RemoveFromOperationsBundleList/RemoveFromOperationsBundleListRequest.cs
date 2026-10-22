namespace Vixly.Operations.Application.UseCases.Bundles.RemoveFromOperationsBundleList;

// =========================================================
// FILE: RemoveFromOperationsBundleListRequest.cs
//
// PURPOSE:
// Take a line off a bundle's list before it is scanned (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveFromOperationsBundleListRequest
{
    public Guid BundleId { get; init; }

    public Guid ItemId { get; init; }
}
