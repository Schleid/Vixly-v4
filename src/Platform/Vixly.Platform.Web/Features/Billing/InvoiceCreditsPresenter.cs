using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Billing.DecidePlatformCreditNote;
using Vixly.Platform.Application.UseCases.Billing.RequestPlatformCreditNote;
using Vixly.Platform.Application.UseCases.Billing.ResendPlatformInvoice;
using Vixly.Platform.Web.Features.Sales;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: InvoiceCreditsPresenter.cs
//
// PURPOSE:
// The Credit notes panel of the invoice page and the resend action (D-B6, D-B13).
//
// RESPONSIBILITIES:
// - Request a credit note or a void with a reason
// - Approve or send back a request made by someone else
// - Send the invoice email again
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// - Four eyes is enforced by the domain; the console only hides the buttons
// =========================================================
public sealed class InvoiceCreditsPresenter : SalesActionPresenter
{
    private readonly RequestPlatformCreditNoteUseCase _request;

    private readonly DecidePlatformCreditNoteUseCase _decide;

    private readonly ResendPlatformInvoiceUseCase _resend;

    public InvoiceCreditsPresenter(
        RequestPlatformCreditNoteUseCase request,
        DecidePlatformCreditNoteUseCase decide,
        ResendPlatformInvoiceUseCase resend,
        PlatformUseCaseScope scope,
        ILogger<InvoiceCreditsPresenter> logger)
        : base(scope, logger)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _decide = decide ?? throw new ArgumentNullException(nameof(decide));
        _resend = resend ?? throw new ArgumentNullException(nameof(resend));
    }

    public Task<bool> RequestAsync(Guid invoiceId, decimal amount, bool isVoid, string? reason)
    {
        return ActAsync(isVoid ? "ui.credit_notes.void_requested" : "ui.credit_notes.requested", async (context, cancellationToken) => ((await _request.ExecuteAsync(
            context, new RequestPlatformCreditNoteRequest { PlatformInvoiceId = invoiceId, Amount = amount, Void = isVoid, Reason = reason }, cancellationToken)).Error, null));
    }

    public Task<bool> DecideAsync(Guid invoiceId, Guid creditId, bool approve, string? reason)
    {
        return ActAsync(approve ? "ui.credit_notes.approved" : "ui.credit_notes.rejected", async (context, cancellationToken) => ((await _decide.ExecuteAsync(
            context, new DecidePlatformCreditNoteRequest { PlatformInvoiceId = invoiceId, CreditId = creditId, Approve = approve, Reason = reason }, cancellationToken)).Error, null));
    }

    public Task<bool> ResendAsync(Guid invoiceId)
    {
        return ActAsync("ui.invoices.resent", async (context, cancellationToken) => ((await _resend.ExecuteAsync(
            context, new ResendPlatformInvoiceRequest { PlatformInvoiceId = invoiceId }, cancellationToken)).Error, null));
    }
}
