using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Domain.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedErrors.cs
//
// PURPOSE:
// The error codes of the sales feed.
//
// RESPONSIBILITIES:
// - Name each refusal with a code (L-1)
//
// IMPORTANT NOTES:
// - Text for each code lives in PlatformText
// =========================================================
public static class PlatformSalesFeedErrors
{
    public static Error EventNotFound { get; } = Error.Create("platform.sales_feed.event_not_found");

    public static Error NotParked { get; } = Error.Create("platform.sales_feed.not_parked");

    public static Error EventInvalid { get; } = Error.Create("platform.sales_feed.event_invalid");

    public static Error RebuildNotPossible { get; } = Error.Create("platform.sales_feed.rebuild_not_possible");
}
