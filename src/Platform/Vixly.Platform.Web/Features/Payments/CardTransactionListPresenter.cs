using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Payments.ListPlatformCardTransactions;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: CardTransactionListPresenter.cs
//
// PURPOSE:
// Behaviour of the card transactions list.
//
// RESPONSIBILITIES:
// - Load a page of charges and refunds for the chosen filters
//
// IMPORTANT NOTES:
// - Filtering and paging happen on the server
// =========================================================
public sealed class CardTransactionListPresenter : PlatformPresenter<CardTransactionListState>
{
    private readonly ListPlatformCardTransactionsUseCase _list;

    public CardTransactionListPresenter(ListPlatformCardTransactionsUseCase list, PlatformUseCaseScope scope, ILogger<CardTransactionListPresenter> logger)
        : base(scope, CardTransactionListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(PlatformCardTransactionStatus? status, PlatformCardTransactionOrigin? origin, bool disputedOnly, int page)
    {
        ListPlatformCardTransactionsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformCardTransactionsRequest { Status = status, Origin = origin, DisputedOnly = disputedOnly, Page = page }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Transactions is { } result
            ? new CardTransactionListState(result.Items, result.Page, result.TotalPages, result.TotalCount, true, null)
            : State with { Loaded = true, Error = error });
    }
}
