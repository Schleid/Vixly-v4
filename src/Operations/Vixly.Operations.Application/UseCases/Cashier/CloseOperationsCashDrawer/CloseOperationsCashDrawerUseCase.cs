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

namespace Vixly.Operations.Application.UseCases.Cashier.CloseOperationsCashDrawer;

// =========================================================
// FILE: CloseOperationsCashDrawerUseCase.cs
//
// PURPOSE:
// Close the person's cash drawer with the count; the difference is recorded with a note (Slice 5F, C-4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.take; only one's own drawer
// =========================================================
public sealed class CloseOperationsCashDrawerUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsPaymentRepository _payments;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CloseOperationsCashDrawerUseCase(
        OperationsBillingDesk desk,
        IOperationsPaymentRepository payments,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CloseOperationsCashDrawerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CloseOperationsCashDrawerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (refused is not null)
        {
            return CloseOperationsCashDrawerResponse.Failure(refused);
        }

        if (await _payments.GetDrawerAsync(request.DrawerId, cancellationToken) is not { } drawer || drawer.UserId != access!.User.Id)
        {
            return CloseOperationsCashDrawerResponse.Failure(OperationsPaymentErrors.DrawerNotFound);
        }

        // Closed twice (a double click): done.
        if (drawer.Status == OperationsCashDrawerStatus.Closed)
        {
            return CloseOperationsCashDrawerResponse.Success();
        }

        try
        {
            drawer.Close(request.Counted, request.Note, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return CloseOperationsCashDrawerResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.payments.drawer_closed", nameof(OperationsCashDrawer), drawer.Id,
            after: new { drawer.Float, drawer.CashIn, drawer.CashOut, drawer.Expected, drawer.Counted, drawer.Difference, note = drawer.CloseNote });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CloseOperationsCashDrawerResponse.Success()
            : CloseOperationsCashDrawerResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
