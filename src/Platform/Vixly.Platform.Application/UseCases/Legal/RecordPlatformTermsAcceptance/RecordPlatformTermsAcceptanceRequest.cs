namespace Vixly.Platform.Application.UseCases.Legal.RecordPlatformTermsAcceptance;

// =========================================================
// FILE: RecordPlatformTermsAcceptanceRequest.cs
//
// PURPOSE:
// Records a tenant's acceptance of the current Terms from a signed paper (D-Lg5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RecordPlatformTermsAcceptanceRequest
{
    public Guid PlatformTenantId { get; init; }

    public string? Language { get; init; }

    public string? OwnerName { get; init; }

    public string? Reason { get; init; }

    public byte[]? Evidence { get; init; }

    public string? EvidenceContentType { get; init; }
}
