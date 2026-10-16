using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformZelleQrCode;

// =========================================================
// FILE: GetPlatformZelleQrCodeUseCase.cs
//
// PURPOSE:
// Serves the Zelle QR code image to the checkout page while Zelle is on.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Public: the image is what customers scan anyway; nothing private is in it
// =========================================================
public sealed class GetPlatformZelleQrCodeUseCase
{
    private readonly PlatformZelleAdministration _zelle;

    public GetPlatformZelleQrCodeUseCase(
        PlatformZelleAdministration zelle)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
    }

    public async Task<GetPlatformZelleQrCodeResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        GetPlatformZelleQrCodeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformZelleImage? image = await _zelle.Queries.GetQrCodeAsync(cancellationToken);

        return image is { Enabled: true }
            ? GetPlatformZelleQrCodeResponse.Success(image)
            : GetPlatformZelleQrCodeResponse.Failure(PlatformZelleErrors.NotAvailable);
    }
}
