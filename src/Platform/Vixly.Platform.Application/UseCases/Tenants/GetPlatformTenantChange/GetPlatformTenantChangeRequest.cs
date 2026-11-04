namespace Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenantChange;

// =========================================================
// FILE: GetPlatformTenantChangeRequest.cs
//
// PURPOSE:
// Asks for one company change request (B12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformTenantChangeRequest
{
    public Guid RequestId { get; init; }
}
