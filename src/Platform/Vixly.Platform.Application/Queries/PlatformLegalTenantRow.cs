namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformLegalTenantRow.cs
//
// PURPOSE:
// A tenant the reminder clock looks at.
//
// RESPONSIBILITIES:
// - Carry the tenant, when it was first activated and its language
//
// IMPORTANT NOTES:
// - Read model
// =========================================================
public sealed record PlatformLegalTenantRow(Guid PlatformTenantId, DateTimeOffset? ActivatedUtc, string Language);
