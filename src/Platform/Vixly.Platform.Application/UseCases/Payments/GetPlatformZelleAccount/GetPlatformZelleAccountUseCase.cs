using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformZelleAccount;

// =========================================================
// FILE: GetPlatformZelleAccountUseCase.cs
//
// PURPOSE:
// Reads Vixly's Zelle details and QR image for the payments settings page.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformZelleAccountUseCase
{
    private readonly PlatformZelleAdministration _zelle;

    public GetPlatformZelleAccountUseCase(
        PlatformZelleAdministration zelle)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
    }

    public async Task<GetPlatformZelleAccountResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformZelleAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _zelle.AuthorizeAsync(context, PlatformPermissions.PaymentsConfigure, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformZelleAccountResponse.Failure(refused);
        }

        PlatformZelleAccountItem account = await _zelle.Queries.GetAccountAsync(cancellationToken)
            ?? new PlatformZelleAccountItem(false, string.Empty, null, null, null, false, 0, null);
        return GetPlatformZelleAccountResponse.Success(account, account.HasQrCode ? await _zelle.Queries.GetQrCodeAsync(cancellationToken) : null);
    }
}
