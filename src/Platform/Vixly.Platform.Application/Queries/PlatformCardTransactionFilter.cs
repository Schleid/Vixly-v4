using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformCardTransactionFilter.cs
//
// PURPOSE:
// Filters of the card transactions list.
//
// RESPONSIBILITIES:
// - Carry status, origin, tenant and invoice filters
//
// IMPORTANT NOTES:
// - All optional
// =========================================================
public sealed record PlatformCardTransactionFilter(
    PlatformCardTransactionStatus? Status,
    PlatformCardTransactionOrigin? Origin,
    Guid? PlatformTenantId,
    Guid? PlatformInvoiceId,
    bool DisputedOnly);
