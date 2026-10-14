using Microsoft.Extensions.Logging;
using Vixly.Shared.Kernel.Errors;
using Vixly.Platform.Application.UseCases.MyAccount.EndMySessions;
using Vixly.Platform.Application.UseCases.MyAccount.GetMyAccount;

namespace Vixly.Platform.Web.Features.Account;

// =========================================================
// FILE: MySessionsPresenter.cs
//
// PURPOSE:
// Behaviour of the sessions card (ADR-0006 section 6).
//
// RESPONSIBILITIES:
// - List the open sessions and mark this one
// - End one other session, or all of them
//
// IMPORTANT NOTES:
// - This session ends by signing out
// =========================================================
public sealed class MySessionsPresenter : PlatformPresenter<MySessionsState>
{
    private readonly GetMyAccountUseCase _account;

    private readonly EndMySessionsUseCase _end;

    public MySessionsPresenter(
        GetMyAccountUseCase account,
        EndMySessionsUseCase end,
        PlatformUseCaseScope scope,
        ILogger<MySessionsPresenter> logger)
        : base(scope, MySessionsState.Initial, logger)
    {
        _account = account ?? throw new ArgumentNullException(nameof(account));
        _end = end ?? throw new ArgumentNullException(nameof(end));
    }

    public async Task LoadAsync()
    {
        GetMyAccountResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _account.ExecuteAsync(context, new GetMyAccountRequest(), cancellationToken);
            return response.Error;
        });

        SetState(response?.Account is { } account
            ? State with { Sessions = account.OpenSessions, CurrentSessionId = response.CurrentSessionId, Error = error }
            : State with { Error = error });
    }

    public async Task EndAsync(Guid? sessionId)
    {
        EndMySessionsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _end.ExecuteAsync(context, new EndMySessionsRequest { SessionId = sessionId }, cancellationToken);
            return response.Error;
        });

        SetState(State with { Error = error, EndedCount = error is null ? response?.EndedCount : null });

        if (error is null)
        {
            await LoadAsync();
        }
    }
}
