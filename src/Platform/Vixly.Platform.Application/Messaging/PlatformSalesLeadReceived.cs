namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformSalesLeadReceived.cs
//
// PURPOSE:
// A contact request arrived (Stage 2).
//
// RESPONSIBILITIES:
// - Carry the lead id
//
// IMPORTANT NOTES:
// - Internal message; the handler emails sales staff
// =========================================================
public sealed record PlatformSalesLeadReceived(Guid LeadId, DateTimeOffset OccurredUtc);
