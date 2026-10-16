using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.ReportRegistrationZellePayment;

// =========================================================
// FILE: ReportRegistrationZellePaymentUseCase.cs
//
// PURPOSE:
// Takes a self-registering company's "I paid with Zelle" report for its first invoice.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous: the registration access token is the proof
// - Registration moves on only when staff confirm and the invoice is paid
// =========================================================
public sealed class ReportRegistrationZellePaymentUseCase
{
    private readonly PlatformZelleAdministration _zelle;

    private readonly PlatformRegistrationLinks _links;

    public ReportRegistrationZellePaymentUseCase(
        PlatformZelleAdministration zelle,
        PlatformRegistrationLinks links)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
        _links = links ?? throw new ArgumentNullException(nameof(links));
    }

    public async Task<ReportRegistrationZellePaymentResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        ReportRegistrationZellePaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformRegistrationAccess access = await _links.OpenAsync(request.AccessToken, _zelle.UtcNow, cancellationToken);

        if (access.Registration is not { PlatformTenantId: Guid tenantId } registration)
        {
            return ReportRegistrationZellePaymentResponse.Failure(access.Error ?? PlatformRegistrationErrors.NotFound);
        }

        if (registration.Status != PlatformTenantRegistrationStatus.PendingPayment
            || await PlatformRegistrationInvoice.FindAsync(_zelle.Payments, tenantId, cancellationToken) is not { } invoice)
        {
            return ReportRegistrationZellePaymentResponse.Failure(PlatformPaymentsErrors.NothingToCharge);
        }

        Error? error = await _zelle.ReportAsync(invoice, PlatformZelleClaimSource.Registration, request.Amount, request.SentOn, request.SenderName, request.Reference, cancellationToken);
        return error is null ? ReportRegistrationZellePaymentResponse.Success() : ReportRegistrationZellePaymentResponse.Failure(error);
    }
}
