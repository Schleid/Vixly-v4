using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.ListPlatformCardTransactions;

// =========================================================
// FILE: ListPlatformCardTransactionsUseCase.cs
//
// PURPOSE:
// Lists card charges and refunds, paged, with status, origin and dispute filters.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListPlatformCardTransactionsUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly IPlatformPaymentsQueries _queries;

    public ListPlatformCardTransactionsUseCase(
        PlatformPaymentsAdministration payments,
        IPlatformPaymentsQueries queries)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListPlatformCardTransactionsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformCardTransactionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return ListPlatformCardTransactionsResponse.Failure(refused);
        }

        return ListPlatformCardTransactionsResponse.Success(await _queries.ListTransactionsAsync(
            new PlatformCardTransactionFilter(request.Status, request.Origin, request.PlatformTenantId, null, request.DisputedOnly),
            Vixly.Shared.Kernel.Paging.PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken));
    }
}
