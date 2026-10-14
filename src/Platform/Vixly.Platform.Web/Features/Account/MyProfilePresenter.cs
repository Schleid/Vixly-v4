using Microsoft.Extensions.Logging;
using Vixly.Shared.Kernel.Errors;
using Vixly.Platform.Application.UseCases.MyAccount.ChangeMyLanguage;
using Vixly.Platform.Application.UseCases.MyAccount.GetMyAccount;

namespace Vixly.Platform.Web.Features.Account;

// =========================================================
// FILE: MyProfilePresenter.cs
//
// PURPOSE:
// Behaviour of the profile card: who you are and your language.
//
// RESPONSIBILITIES:
// - Load the account
// - Change the preferred language
//
// IMPORTANT NOTES:
// - The component reloads the page after a change, so the whole
//   console switches language
// =========================================================
public sealed class MyProfilePresenter : PlatformPresenter<MyProfileState>
{
    private readonly GetMyAccountUseCase _account;

    private readonly ChangeMyLanguageUseCase _language;

    public MyProfilePresenter(
        GetMyAccountUseCase account,
        ChangeMyLanguageUseCase language,
        PlatformUseCaseScope scope,
        ILogger<MyProfilePresenter> logger)
        : base(scope, MyProfileState.Initial, logger)
    {
        _account = account ?? throw new ArgumentNullException(nameof(account));
        _language = language ?? throw new ArgumentNullException(nameof(language));
    }

    public async Task LoadAsync()
    {
        GetMyAccountResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _account.ExecuteAsync(context, new GetMyAccountRequest(), cancellationToken);
            return response.Error;
        });

        SetState(State with { Account = response?.Account ?? State.Account, Error = error });
    }

    public async Task ChangeLanguageAsync(string language)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
            (await _language.ExecuteAsync(context, new ChangeMyLanguageRequest { Language = language ?? string.Empty }, cancellationToken)).Error);

        SetState(State with { Error = error, LanguageChanged = error is null });
    }
}
