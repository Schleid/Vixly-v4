namespace Vixly.Platform.Application.UseCases.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedParkedItem.cs
//
// PURPOSE:
// One sale event TechOS rejected, as the Control sales feed page lists it.
//
// RESPONSIBILITIES:
// - Carry the id, sequence, type, time, tenant, TechOS's error and when it was answered
//
// IMPORTANT NOTES:
// - Plain record; never the event body
// =========================================================
public sealed record PlatformSalesFeedParkedItem(
    Guid Id,
    long Sequence,
    string Type,
    DateTimeOffset OccurredUtc,
    Guid? PlatformTenantId,
    string? Error,
    DateTimeOffset? AnsweredUtc);
