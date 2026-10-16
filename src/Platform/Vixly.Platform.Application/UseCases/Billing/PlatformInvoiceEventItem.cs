namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformInvoiceEventItem.cs
//
// PURPOSE:
// One invoice history entry as shown.
//
// RESPONSIBILITIES:
// - Carry what the console shows
//
// IMPORTANT NOTES:
// - Plain record for the console
// =========================================================
public sealed record PlatformInvoiceEventItem(
    string EventCode,
    IReadOnlyDictionary<string, string> Arguments,
    string? ActorName,
    string? Reason,
    DateTimeOffset OccurredUtc);
