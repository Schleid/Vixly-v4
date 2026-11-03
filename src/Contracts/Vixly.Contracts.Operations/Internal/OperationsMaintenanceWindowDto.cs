namespace Vixly.Contracts.Operations.Internal;

// =========================================================
// FILE: OperationsMaintenanceWindowDto.cs
//
// PURPOSE:
// A maintenance window of the customer portals, as Operations.Api tells Customer.Web (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Carry the times and the message in en, fr and es
//
// IMPORTANT NOTES:
// - Times are UTC
// - Additive changes only (cross-plane rule 4)
// =========================================================
public sealed record OperationsMaintenanceWindowDto(Guid Id, DateTimeOffset StartsUtc, DateTimeOffset EndsUtc, string? MessageEn, string? MessageFr, string? MessageEs);
