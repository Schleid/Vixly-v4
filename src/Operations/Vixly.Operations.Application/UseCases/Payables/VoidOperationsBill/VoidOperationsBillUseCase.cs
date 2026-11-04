using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Payables;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Payables;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Payables.VoidOperationsBill;

// =========================================================
// FILE: VoidOperationsBillUseCase.cs
//
// PURPOSE:
// Voids a bill entered by mistake, while nothing is paid on it (Slice 8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bills.approve; a reason is required
// =========================================================
public sealed class VoidOperationsBillUseCase
{
    private readonly OperationsPayablesDesk _desk;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public VoidOperationsBillUseCase(
        OperationsPayablesDesk desk,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<VoidOperationsBillResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        VoidOperationsBillRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _desk.RequireAsync(context, OperationsPermissions.BillsApprove, false, cancellationToken);

        if (refused is not null)
        {
            return VoidOperationsBillResponse.Failure(refused);
        }

        if (await _desk.Payables.GetBillAsync(request.BillId, cancellationToken) is not { } bill)
        {
            return VoidOperationsBillResponse.Failure(OperationsPayableErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            bill.Void(request.Reason, access!.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return VoidOperationsBillResponse.Failure(exception.Error);
        }

        await _desk.ApplyAsync(bill, bill.Allocations.Where(allocation => allocation.DispatchId is not null).Select(allocation => allocation.DispatchId!.Value), utcNow,
            cancellationToken);
        _audit.Record(access.User.Id, "ops.bills.voided", nameof(OperationsBill), bill.Id, after: new { party = bill.PartyName, bill.Reference, bill.Amount, reason = bill.VoidReason });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return VoidOperationsBillResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return VoidOperationsBillResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
