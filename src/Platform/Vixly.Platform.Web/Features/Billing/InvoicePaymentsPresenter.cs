using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Billing.RecordPlatformPayment;
using Vixly.Platform.Application.UseCases.Billing.ReversePlatformPayment;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Web.Features.Sales;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: InvoicePaymentsPresenter.cs
//
// PURPOSE:
// The Payments panel of the invoice page (D-B7).
//
// RESPONSIBILITIES:
// - Record a payment received by hand; reverse a recorded one with a reason
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// - Both actions ask for step-up; the use case decides
// - A date in the past is taken at noon UTC; today means now
// =========================================================
public sealed class InvoicePaymentsPresenter : SalesActionPresenter
{
    private readonly RecordPlatformPaymentUseCase _record;

    private readonly ReversePlatformPaymentUseCase _reverse;

    private readonly TimeProvider _clock;

    public InvoicePaymentsPresenter(RecordPlatformPaymentUseCase record, ReversePlatformPaymentUseCase reverse, TimeProvider clock, PlatformUseCaseScope scope, ILogger<InvoicePaymentsPresenter> logger)
        : base(scope, logger)
    {
        _record = record ?? throw new ArgumentNullException(nameof(record));
        _reverse = reverse ?? throw new ArgumentNullException(nameof(reverse));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Task<bool> RecordAsync(Guid invoiceId, decimal amount, PlatformPaymentMethod method, DateTime? received, string? reference, string? note)
    {
        DateTimeOffset now = _clock.GetUtcNow();
        DateTimeOffset receivedUtc = received is not DateTime day || day.Date >= now.UtcDateTime.Date
            ? now
            : new DateTimeOffset(DateTime.SpecifyKind(day.Date.AddHours(12), DateTimeKind.Utc));

        return ActAsync("ui.payments.recorded", async (context, cancellationToken) => ((await _record.ExecuteAsync(
            context,
            new RecordPlatformPaymentRequest { PlatformInvoiceId = invoiceId, Amount = amount, Method = method, ReceivedUtc = receivedUtc, Reference = reference, Note = note },
            cancellationToken)).Error, null));
    }

    public Task<bool> ReverseAsync(Guid invoiceId, Guid paymentId, string? reason)
    {
        return ActAsync("ui.payments.reversed", async (context, cancellationToken) => ((await _reverse.ExecuteAsync(
            context, new ReversePlatformPaymentRequest { PlatformInvoiceId = invoiceId, PaymentId = paymentId, Reason = reason }, cancellationToken)).Error, null));
    }
}
