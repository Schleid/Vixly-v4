using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.OpenOperationsCashDrawer;

// =========================================================
// FILE: OpenOperationsCashDrawerUseCase.cs
//
// PURPOSE:
// Open the person's cash drawer at a warehouse with a starting float (Slice 5F, C-4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.take, at that warehouse
// - One open drawer per person and warehouse
// =========================================================
public sealed class OpenOperationsCashDrawerUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsPaymentRepository _payments;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OpenOperationsCashDrawerUseCase(
        OperationsBillingDesk desk,
        IOperationsPaymentRepository payments,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<OpenOperationsCashDrawerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        OpenOperationsCashDrawerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (refused is not null)
        {
            return OpenOperationsCashDrawerResponse.Failure(refused);
        }

        if (warehouses.All(warehouse => warehouse.Id != request.WarehouseId))
        {
            return OpenOperationsCashDrawerResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // No cash accepted, no drawer (P-5).
        if ((await _settings.FindAsync(cancellationToken))?.AcceptCash == false)
        {
            return OpenOperationsCashDrawerResponse.Failure(OperationsPaymentErrors.MethodNotAccepted);
        }

        // Already open (a double click): the same drawer.
        if (await _payments.FindOpenDrawerAsync(access!.User.Id, request.WarehouseId, cancellationToken) is { } open)
        {
            return OpenOperationsCashDrawerResponse.Success(open.Id);
        }

        OperationsCashDrawer drawer;

        try
        {
            drawer = OperationsCashDrawer.Open(context.RequiredTenantId, request.WarehouseId, access.User.Id, request.Float, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return OpenOperationsCashDrawerResponse.Failure(exception.Error);
        }

        _payments.Add(drawer);
        _audit.Record(access.User.Id, "ops.payments.drawer_opened", nameof(OperationsCashDrawer), drawer.Id, after: new { warehouse = request.WarehouseId, drawer.Float });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return OpenOperationsCashDrawerResponse.Success(drawer.Id);
        }

        _unitOfWork.DiscardChanges();
        return await _payments.FindOpenDrawerAsync(access.User.Id, request.WarehouseId, cancellationToken) is { } made
            ? OpenOperationsCashDrawerResponse.Success(made.Id)
            : OpenOperationsCashDrawerResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
