using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Audit.ListPlatformSignIns;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Audit;

// =========================================================
// FILE: SignInActivityPresenter.cs
//
// PURPOSE:
// Loads member activity: sign-ins to Control and how they ended
// (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Run ListPlatformSignIns for one user or all, one page at a time
//
// IMPORTANT NOTES:
// - Presenter (ADR-0001): no markup
// =========================================================
public sealed class SignInActivityPresenter : PlatformPresenter<SignInActivityState>
{
    private readonly ListPlatformSignInsUseCase _list;

    public SignInActivityPresenter(
        ListPlatformSignInsUseCase list,
        PlatformUseCaseScope scope,
        ILogger<SignInActivityPresenter> logger)
        : base(scope, SignInActivityState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(Guid? userId, int page)
    {
        ListPlatformSignInsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformSignInsRequest { PlatformUserId = userId, Page = Math.Max(page, 1) }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.SignIns is { } signIns
            ? new SignInActivityState(true, signIns.Items, signIns.Page, signIns.TotalPages, signIns.TotalCount, null)
            : SignInActivityState.Initial with { Loaded = true, Error = error });
    }
}
