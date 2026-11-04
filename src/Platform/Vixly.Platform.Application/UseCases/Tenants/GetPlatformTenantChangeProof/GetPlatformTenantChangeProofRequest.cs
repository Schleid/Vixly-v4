namespace Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenantChangeProof;

// =========================================================
// FILE: GetPlatformTenantChangeProofRequest.cs
//
// PURPOSE:
// Asks for a change request’s proof document (B12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformTenantChangeProofRequest
{
    public Guid RequestId { get; init; }
}
