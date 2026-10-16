using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformZelleClaim;

// =========================================================
// FILE: GetPlatformZelleClaimUseCase.cs
//
// PURPOSE:
// Reads one Zelle report with the invoice balance and what the reader may do.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformZelleClaimUseCase
{
    private readonly PlatformZelleAdministration _zelle;

    public GetPlatformZelleClaimUseCase(
        PlatformZelleAdministration zelle)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
    }

    public async Task<GetPlatformZelleClaimResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformZelleClaimRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _zelle.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformZelleClaimResponse.Failure(refused);
        }

        PlatformZelleClaimItem? claim = await _zelle.Queries.GetClaimAsync(request.ClaimId, cancellationToken);

        return claim is null
            ? GetPlatformZelleClaimResponse.Failure(PlatformZelleErrors.ClaimNotFound)
            : GetPlatformZelleClaimResponse.Success(new PlatformZelleClaimDetail(claim, new PlatformZelleClaimAbilities(
                claim.Status == PlatformZelleClaimStatus.Waiting && await _zelle.HoldsAsync(context, PlatformPermissions.BillingRecordPayments, cancellationToken))));
    }
}
