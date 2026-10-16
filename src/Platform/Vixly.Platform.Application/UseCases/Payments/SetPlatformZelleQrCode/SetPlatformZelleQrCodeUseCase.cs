using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.SetPlatformZelleQrCode;

// =========================================================
// FILE: SetPlatformZelleQrCodeUseCase.cs
//
// PURPOSE:
// Replaces or removes the Zelle QR code image shown at checkout.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SetPlatformZelleQrCodeUseCase
{
    private readonly PlatformZelleAdministration _zelle;

    public SetPlatformZelleQrCodeUseCase(
        PlatformZelleAdministration zelle)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
    }

    public async Task<SetPlatformZelleQrCodeResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SetPlatformZelleQrCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _zelle.AuthorizeAsync(context, PlatformPermissions.PaymentsConfigure, requireStepUp: true, cancellationToken) is { } refused)
        {
            return SetPlatformZelleQrCodeResponse.Failure(refused);
        }

        PlatformZelleAccount account = await _zelle.AccountAsync(cancellationToken);

        try
        {
            account.SetQrCode(request.Content, request.ContentType, context.PlatformUserId, _zelle.UtcNow);
        }
        catch (DomainException exception)
        {
            return SetPlatformZelleQrCodeResponse.Failure(exception.Error);
        }

        _zelle.Payments.Audit(context, "platform.payments.zelle_qr_updated", PlatformPermissions.PaymentsConfigure, nameof(PlatformZelleAccount), account.Id,
            new { removed = !account.HasQrCode, version = account.QrCodeVersion });

        return await _zelle.SaveAsync(cancellationToken)
            ? SetPlatformZelleQrCodeResponse.Success()
            : SetPlatformZelleQrCodeResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
