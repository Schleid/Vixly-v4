namespace Vixly.Platform.Application.UseCases.Tenants.ApprovePlatformTenantChange;

// =========================================================
// FILE: ApprovePlatformTenantChangeRequest.cs
//
// PURPOSE:
// Staff decision to approve a company change request (B12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ApprovePlatformTenantChangeRequest
{
    public Guid RequestId { get; init; }

    public string? Note { get; init; }
}
