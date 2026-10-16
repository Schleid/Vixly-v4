using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Billing.GetPlatformInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: InvoicePresenter.cs
//
// PURPOSE:
// Behaviour of the invoice page.
//
// RESPONSIBILITIES:
// - Load one invoice with its lines, payments, credit notes and history
//
// IMPORTANT NOTES:
// - Actions live in InvoicePaymentsPresenter and InvoiceCreditsPresenter
// =========================================================
public sealed class InvoicePresenter : PlatformPresenter<InvoiceState>
{
    private readonly GetPlatformInvoiceUseCase _get;

    public InvoicePresenter(GetPlatformInvoiceUseCase get, PlatformUseCaseScope scope, ILogger<InvoicePresenter> logger)
        : base(scope, InvoiceState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
    }

    public async Task LoadAsync(Guid invoiceId)
    {
        GetPlatformInvoiceResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformInvoiceRequest { PlatformInvoiceId = invoiceId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? new InvoiceState(response.Invoice, response.Abilities, true, State.Version + 1, null)
            : State with { Loaded = true, Error = error });
    }
}
