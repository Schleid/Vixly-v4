using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalRegister;
using Vixly.Platform.Domain.Legal;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Legal;

// =========================================================
// FILE: LegalRegisterPresenter.cs
//
// PURPOSE:
// Behaviour of the acceptance register (D-Lg12).
//
// RESPONSIBILITIES:
// - Load one page of tenants that must accept the current required Terms
//
// IMPORTANT NOTES:
// - Read only; actions live on the tenant page
// =========================================================
public sealed class LegalRegisterPresenter : PlatformPresenter<LegalRegisterState>
{
    private readonly GetPlatformLegalRegisterUseCase _register;

    public LegalRegisterPresenter(GetPlatformLegalRegisterUseCase register, PlatformUseCaseScope scope, ILogger<LegalRegisterPresenter> logger)
        : base(scope, LegalRegisterState.Initial, logger)
    {
        _register = register ?? throw new ArgumentNullException(nameof(register));
    }

    public async Task LoadAsync(PlatformLegalStatus? status, string? search, int page)
    {
        GetPlatformLegalRegisterResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _register.ExecuteAsync(context, new GetPlatformLegalRegisterRequest { Status = status, Search = search, Page = page }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Tenants is { } result
            ? new LegalRegisterState(response.Required, result.Items, result.Page, result.TotalPages, result.TotalCount, true, null)
            : State with { Loaded = true, Error = error });
    }
}
