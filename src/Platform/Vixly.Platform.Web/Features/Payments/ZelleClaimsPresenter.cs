using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Payments.ListPlatformZelleClaims;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: ZelleClaimsPresenter.cs
//
// PURPOSE:
// Behaviour of the Zelle reports list: staff's queue of payments to check.
//
// RESPONSIBILITIES:
// - Load one page of reports with the chosen filters
//
// IMPORTANT NOTES:
// - Read only; decisions are taken on the report page
// =========================================================
public sealed class ZelleClaimsPresenter : PlatformPresenter<ZelleClaimsState>
{
    private readonly ListPlatformZelleClaimsUseCase _list;

    public ZelleClaimsPresenter(ListPlatformZelleClaimsUseCase list, PlatformUseCaseScope scope, ILogger<ZelleClaimsPresenter> logger)
        : base(scope, ZelleClaimsState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(PlatformZelleClaimStatus? status, string? search, int page)
    {
        ListPlatformZelleClaimsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformZelleClaimsRequest { Status = status, Search = search, Page = page }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Claims is { } result
            ? new ZelleClaimsState(result.Items, result.Page, result.TotalPages, result.TotalCount, true, null)
            : State with { Loaded = true, Error = error });
    }
}
