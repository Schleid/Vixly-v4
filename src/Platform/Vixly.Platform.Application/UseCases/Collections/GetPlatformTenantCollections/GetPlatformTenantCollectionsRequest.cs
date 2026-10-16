namespace Vixly.Platform.Application.UseCases.Collections.GetPlatformTenantCollections;

// =========================================================
// FILE: GetPlatformTenantCollectionsRequest.cs
//
// PURPOSE:
// Reads the Collections section of a tenant page: the open case and overdue invoices.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformTenantCollectionsRequest
{
    public Guid PlatformTenantId { get; init; }
}
