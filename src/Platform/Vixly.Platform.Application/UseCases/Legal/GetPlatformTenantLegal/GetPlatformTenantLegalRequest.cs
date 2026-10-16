namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformTenantLegal;

// =========================================================
// FILE: GetPlatformTenantLegalRequest.cs
//
// PURPOSE:
// Reads the Legal section of a tenant page: status, acceptances, reminders and actions.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformTenantLegalRequest
{
    public Guid PlatformTenantId { get; init; }
}
