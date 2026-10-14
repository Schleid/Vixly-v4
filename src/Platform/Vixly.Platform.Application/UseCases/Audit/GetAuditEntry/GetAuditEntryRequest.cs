namespace Vixly.Platform.Application.UseCases.Audit.GetAuditEntry;

// =========================================================
// FILE: GetAuditEntryRequest.cs
//
// PURPOSE:
// Input for GetAuditEntry.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class GetAuditEntryRequest
{
    public Guid AuditEntryId { get; init; }
}
