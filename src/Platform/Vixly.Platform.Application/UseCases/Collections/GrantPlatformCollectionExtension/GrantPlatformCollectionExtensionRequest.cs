namespace Vixly.Platform.Application.UseCases.Collections.GrantPlatformCollectionExtension;

// =========================================================
// FILE: GrantPlatformCollectionExtensionRequest.cs
//
// PURPOSE:
// Gives a tenant more time: reminders pause and a block is lifted until the date (D-C7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GrantPlatformCollectionExtensionRequest
{
    public Guid CaseId { get; init; }

    public DateTime Through { get; init; }

    public string? Reason { get; init; }
}
