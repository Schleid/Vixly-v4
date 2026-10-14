namespace Vixly.Platform.Application.UseCases.Registration.CheckPortalSlugAvailability;

// =========================================================
// FILE: CheckPortalSlugAvailabilityRequest.cs
//
// PURPOSE:
// Input for CheckPortalSlugAvailability.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - As typed; normalised here
// =========================================================
public sealed class CheckPortalSlugAvailabilityRequest
{
    public string Slug { get; init; } = string.Empty;
}
