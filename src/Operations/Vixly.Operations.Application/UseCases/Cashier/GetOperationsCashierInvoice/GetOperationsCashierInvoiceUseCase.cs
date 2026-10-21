using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashierInvoice;

// =========================================================
// FILE: GetOperationsCashierInvoiceUseCase.cs
//
// PURPOSE:
// One invoice at the cashier (Slice 5F, C-2 to C-5): what is owed, the methods the company accepts, the Zelle details and terms, the person's drawer, the payments so far.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.take, at the invoice's warehouse
// =========================================================
public sealed class GetOperationsCashierInvoiceUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsPaymentRepository _payments;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsCreditRepository _credit;

    private readonly OperationsCreditBook _book;

    private readonly TimeProvider _timeProvider;

    public GetOperationsCashierInvoiceUseCase(
        OperationsBillingDesk desk,
        IOperationsBillingRepository billing,
        IOperationsPaymentRepository payments,
        IOperationsSettingsRepository settings,
        IOperationsCreditRepository credit,
        OperationsCreditBook book,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _credit = credit ?? throw new ArgumentNullException(nameof(credit));
        _book = book ?? throw new ArgumentNullException(nameof(book));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsCashierInvoiceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCashierInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCashierInvoiceResponse.Failure(refused);
        }

        if (await _billing.GetInvoiceAsync(request.InvoiceId, cancellationToken) is not { } invoice || warehouses.All(warehouse => warehouse.Id != invoice.WarehouseId))
        {
            return GetOperationsCashierInvoiceResponse.Failure(OperationsBillingErrors.InvoiceNotFound);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        OperationsWarehouse at = warehouses.First(warehouse => warehouse.Id == invoice.WarehouseId);
        OperationsCreditAccount? account = settings.AllowCredit ? await _credit.FindByCustomerAsync(invoice.CustomerId, cancellationToken) : null;
        OperationsCreditPosition? position = account is null ? null
            : (await _book.ReadAsync(account, invoice.CustomerId, OperationsCreditBook.Today(settings, _timeProvider.GetUtcNow()), cancellationToken)).Position;
        return GetOperationsCashierInvoiceResponse.Success(invoice, settings, OperationsPaymentNames.Accepted(settings),
            await _payments.FindOpenDrawerAsync(access!.User.Id, invoice.WarehouseId, cancellationToken),
            await _payments.ListInvoicePaymentsAsync(invoice.Id, cancellationToken), await _payments.ListInvoiceRefundsAsync(invoice.Id, cancellationToken),
            invoice.Refundable > 0m && access.CoversWarehouse(OperationsPermissions.PaymentsRefund, at.Id, at.BranchId), account, position);
    }
}
