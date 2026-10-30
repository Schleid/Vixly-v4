using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsPayment;

// =========================================================
// FILE: GetOperationsPaymentUseCase.cs
//
// PURPOSE:
// A payment just taken (Slice 5F, C-3): its tenders, the change to give and its invoice.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.take, at the payment's warehouse
// =========================================================
public sealed class GetOperationsPaymentUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsPaymentRepository _payments;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsPaymentUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsPaymentRepository payments,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsPaymentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsPaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsPaymentResponse.Failure(refused);
        }

        if (await _payments.GetPaymentAsync(request.PaymentId, cancellationToken) is not { } payment || warehouses.All(warehouse => warehouse.Id != payment.WarehouseId)
            || await _billing.GetInvoiceAsync(payment.InvoiceId, cancellationToken) is not { } invoice)
        {
            return GetOperationsPaymentResponse.Failure(OperationsPaymentErrors.NotFound);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        return GetOperationsPaymentResponse.Success(payment, invoice, OperationsPaymentNames.Others(settings));
    }
}
