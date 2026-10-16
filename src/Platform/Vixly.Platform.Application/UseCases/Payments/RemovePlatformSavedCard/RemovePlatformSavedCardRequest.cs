namespace Vixly.Platform.Application.UseCases.Payments.RemovePlatformSavedCard;

// =========================================================
// FILE: RemovePlatformSavedCardRequest.cs
//
// PURPOSE:
// Removes a saved card and detaches it at the provider (D-P3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemovePlatformSavedCardRequest
{
    public Guid PlatformTenantId { get; init; }

    public Guid SavedCardId { get; init; }
}
