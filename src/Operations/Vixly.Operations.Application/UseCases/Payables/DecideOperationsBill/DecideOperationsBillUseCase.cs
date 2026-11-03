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

namespace Vixly.Operations.Application.UseCases.Payables.DecideOperationsBill;

// =========================================================
// FILE: DecideOperationsBillUseCase.cs
//
// PURPOSE:
// Approves a bill, or sends it back with a reason (Slice 8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bills.approve; never by the person who entered it while the company asks for approval
// =========================================================
public sealed class DecideOperationsBillUseCase
{
    private readonly OperationsPayablesDesk _desk;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public DecideOperationsBillUseCase(
        OperationsPayablesDesk desk,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DecideOperationsBillResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        DecideOperationsBillRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _desk.RequireAsync(context, OperationsPermissions.BillsApprove, false, cancellationToken);

        if (refused is not null)
        {
            return DecideOperationsBillResponse.Failure(refused);
        }

        if (await _desk.Payables.GetBillAsync(request.BillId, cancellationToken) is not { } bill)
        {
            return DecideOperationsBillResponse.Failure(OperationsPayableErrors.NotFound);
        }

        if (!string.Equals(request.Version, OperationsPayablesDesk.Version(bill), StringComparison.Ordinal))
        {
            return DecideOperationsBillResponse.Failure(OperationsPayableErrors.Changed);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        bool needApproval = (await _settings.FindAsync(cancellationToken))?.BillsNeedApproval ?? true;

        try
        {
            if (request.Approve)
            {
                bill.Approve(access!.User.Id, !needApproval, utcNow);
            }
            else
            {
                bill.Reject(access!.User.Id, request.Reason, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return DecideOperationsBillResponse.Failure(exception.Error);
        }

        await _desk.ApplyAsync(bill, bill.Allocations.Where(allocation => allocation.DispatchId is not null).Select(allocation => allocation.DispatchId!.Value), utcNow,
            cancellationToken);
        _audit.Record(access.User.Id, request.Approve ? "ops.bills.approved" : "ops.bills.rejected", nameof(OperationsBill), bill.Id,
            after: new { party = bill.PartyName, bill.Reference, bill.Amount, reason = bill.RejectReason });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return DecideOperationsBillResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return DecideOperationsBillResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
