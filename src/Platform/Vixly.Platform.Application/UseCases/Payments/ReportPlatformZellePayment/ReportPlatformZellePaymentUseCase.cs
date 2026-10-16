using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.ReportPlatformZellePayment;

// =========================================================
// FILE: ReportPlatformZellePaymentUseCase.cs
//
// PURPOSE:
// Takes a customer's "I paid with Zelle" report from a pay link; staff check it before anything is paid.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous: the pay link token is the proof
// - The invoice stays unpaid until staff confirm
// =========================================================
public sealed class ReportPlatformZellePaymentUseCase
{
    private readonly PlatformZelleAdministration _zelle;

    public ReportPlatformZellePaymentUseCase(
        PlatformZelleAdministration zelle)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
    }

    public async Task<ReportPlatformZellePaymentResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        ReportPlatformZellePaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPaymentLink? link = await _zelle.Payments.OpenLinkAsync(request.Token, cancellationToken);

        if (link is not { Kind: PlatformPaymentLinkKind.PayInvoice, PlatformInvoiceId: Guid invoiceId }
            || await _zelle.Payments.Billing.Invoices.GetAsync(invoiceId, cancellationToken) is not { } invoice)
        {
            return ReportPlatformZellePaymentResponse.Failure(PlatformPaymentsErrors.LinkInvalid);
        }

        Error? error = await _zelle.ReportAsync(invoice, PlatformZelleClaimSource.PaymentLink, request.Amount, request.SentOn, request.SenderName, request.Reference, cancellationToken);
        return error is null ? ReportPlatformZellePaymentResponse.Success() : ReportPlatformZellePaymentResponse.Failure(error);
    }
}
