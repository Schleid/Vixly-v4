namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformTenantLegalStatus;

// =========================================================
// FILE: GetPlatformTenantLegalStatusRequest.cs
//
// PURPOSE:
// Tells Operations where its tenant stands with the Terms and what to show the owner (D-Lg11).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformTenantLegalStatusRequest
{
    public string? Language { get; init; }
}
