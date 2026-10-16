using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.UseCases.Payments.ListPlatformCardTransactions;

// =========================================================
// FILE: ListPlatformCardTransactionsRequest.cs
//
// PURPOSE:
// Lists card charges and refunds, paged, with status, origin and dispute filters.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformCardTransactionsRequest
{
    public PlatformCardTransactionStatus? Status { get; init; }

    public PlatformCardTransactionOrigin? Origin { get; init; }

    public Guid? PlatformTenantId { get; init; }

    public bool DisputedOnly { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
