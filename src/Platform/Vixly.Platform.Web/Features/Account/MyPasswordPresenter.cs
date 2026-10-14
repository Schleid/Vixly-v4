using Microsoft.Extensions.Logging;
using Vixly.Shared.Kernel.Errors;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.UseCases.MyAccount.ChangeMyPassword;

namespace Vixly.Platform.Web.Features.Account;

// =========================================================
// FILE: MyPasswordPresenter.cs
//
// PURPOSE:
// Behaviour of the password card.
//
// RESPONSIBILITIES:
// - Refuse a confirmation that differs from the new password
// - Change the password; report sessions ended elsewhere
//
// IMPORTANT NOTES:
// - Passwords live only in the request; never kept in state
// =========================================================
public sealed class MyPasswordPresenter : PlatformPresenter<MyPasswordState>
{
    private static readonly Error ConfirmationDiffers = Error.Create("ui.account.password_mismatch");

    private readonly ChangeMyPasswordUseCase _change;

    public MyPasswordPresenter(
        ChangeMyPasswordUseCase change,
        PlatformUseCaseScope scope,
        ILogger<MyPasswordPresenter> logger)
        : base(scope, MyPasswordState.Initial, logger)
    {
        _change = change ?? throw new ArgumentNullException(nameof(change));
    }

    public async Task<bool> ChangeAsync(string current, string replacement, string confirmation)
    {
        if (!string.Equals(replacement, confirmation, StringComparison.Ordinal))
        {
            SetState(MyPasswordState.Initial with { Error = ConfirmationDiffers });
            return false;
        }

        ChangeMyPasswordResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _change.ExecuteAsync(
                context,
                new ChangeMyPasswordRequest { CurrentPassword = current ?? string.Empty, NewPassword = replacement ?? string.Empty },
                cancellationToken);

            return response.Error;
        });

        SetState(new MyPasswordState(
            error,
            error is null,
            error is null ? response?.OtherSessionsEnded ?? 0 : 0,
            PlatformAuthErrors.SessionEnded.Equals(error)));

        return error is null;
    }
}
