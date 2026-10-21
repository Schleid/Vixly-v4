using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using System.Globalization;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsPaymentReceipt;

// =========================================================
// FILE: GetOperationsPaymentReceiptUseCase.cs
//
// PURPOSE:
// The receipt of a payment as a PDF, in the customer's language (Slice 5F, C-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.take or ops.invoices.view, at the payment's warehouse
// =========================================================
public sealed class GetOperationsPaymentReceiptUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsPaymentRepository _payments;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly TimeProvider _timeProvider;

    public GetOperationsPaymentReceiptUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsPaymentRepository payments,
        IOperationsSettingsRepository settings,
        IOperationsLabelRenderer renderer,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsPaymentReceiptResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsPaymentReceiptRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> take, Error? _) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (access is null)
        {
            return GetOperationsPaymentReceiptResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        // Those who see the invoices print their receipts too.
        IReadOnlyList<OperationsWarehouse> seen = take.Concat(await _desk.WarehousesAsync(access, OperationsPermissions.InvoicesView, cancellationToken)).ToList();

        if (await _payments.GetPaymentAsync(request.PaymentId, cancellationToken) is not { } payment || seen.All(warehouse => warehouse.Id != payment.WarehouseId)
            || await _billing.GetInvoiceAsync(payment.InvoiceId, cancellationToken) is not { } invoice)
        {
            return GetOperationsPaymentReceiptResponse.Failure(OperationsPaymentErrors.NotFound);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        CultureInfo before = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(invoice.Language);
            return GetOperationsPaymentReceiptResponse.Success(_renderer.RenderPaymentReceipt(new OperationsPaymentReceipt(payment, invoice, OperationsPaymentNames.Others(settings),
                OperationsTimeZones.Today(payment.TakenUtc, settings.TimeZone))),
                payment.Number + ".pdf");
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }
}
