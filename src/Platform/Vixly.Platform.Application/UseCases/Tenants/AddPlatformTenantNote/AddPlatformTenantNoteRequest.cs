namespace Vixly.Platform.Application.UseCases.Tenants.AddPlatformTenantNote;

// =========================================================
// FILE: AddPlatformTenantNoteRequest.cs
//
// PURPOSE:
// Input for AddPlatformTenantNote.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the domain
// =========================================================
public sealed class AddPlatformTenantNoteRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Body { get; init; } = string.Empty;
}
