using System.Globalization;
using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Sales.ListPlatformApprovals;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: ApprovalListPresenter.cs
//
// PURPOSE:
// Lists what waits for the current person's approval (D-S24, D-S26).
//
// RESPONSIBILITIES:
// - Run the use cases for its component and hold the state
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// =========================================================
public sealed class ApprovalListPresenter : PlatformPresenter<ApprovalListState>
{
    private readonly ListPlatformApprovalsUseCase _list;

    public ApprovalListPresenter(ListPlatformApprovalsUseCase list, PlatformUseCaseScope scope, ILogger<ApprovalListPresenter> logger)
        : base(scope, ApprovalListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync()
    {
        ListPlatformApprovalsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformApprovalsRequest { Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? new ApprovalListState(response.Items, true, null)
            : State with { Loaded = true, Error = error });
    }
}
