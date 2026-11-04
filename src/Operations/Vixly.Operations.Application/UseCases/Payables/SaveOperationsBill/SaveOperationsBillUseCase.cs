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

namespace Vixly.Operations.Application.UseCases.Payables.SaveOperationsBill;

// =========================================================
// FILE: SaveOperationsBillUseCase.cs
//
// PURPOSE:
// Enters a bill, or corrects one not yet approved (Slice 8): who sent it, their number, what for, dates, amount, and its shares on dispatches or shipments.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bills.enter; a correction only before approval (or after a rejection)
// - The company’s setting decides whether the bill waits for someone else’s approval
// =========================================================
public sealed class SaveOperationsBillUseCase
{
    private readonly OperationsPayablesDesk _desk;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLocationRepository _locations;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsBillUseCase(
        OperationsPayablesDesk desk,
        IOperationsSettingsRepository settings,
        IOperationsLocationRepository locations,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsBillResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsBillRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _desk.RequireAsync(context, OperationsPermissions.BillsEnter, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsBillResponse.Failure(refused);
        }

        string? partyName = request.Party switch
        {
            OperationsBillParty.Carrier => (await _desk.Dispatches.GetCarrierAsync(request.PartyId, cancellationToken))?.Name,
            OperationsBillParty.Partner => await _locations.GetWarehouseAsync(request.PartyId, cancellationToken) is { OperatedBy: OperationsDestinationOperator.Partner } partner
                ? partner.PartnerName ?? partner.Name : null,
            OperationsBillParty.Vendor => (await _desk.Payables.GetVendorAsync(request.PartyId, cancellationToken))?.Name,
            _ => null
        };

        if (partyName is null)
        {
            return SaveOperationsBillResponse.Failure(OperationsPayableErrors.PartyRequired);
        }

        string reference = (request.Reference ?? string.Empty).Trim();

        if (reference.Length > 0 && await _desk.Payables.ReferenceTakenAsync(request.Party, request.PartyId, reference, request.BillId, cancellationToken))
        {
            return SaveOperationsBillResponse.Failure(OperationsPayableErrors.ReferenceTaken);
        }

        (IReadOnlyList<OperationsBillAllocation>? allocations, Error? wrong) = await _desk.AllocationsAsync(request.Shares,
            request.Party == OperationsBillParty.Carrier ? request.PartyId : null, cancellationToken);

        if (wrong is not null)
        {
            return SaveOperationsBillResponse.Failure(wrong);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
        OperationsBill bill;
        List<Guid> touched = new();

        try
        {
            if (request.BillId is { } billId)
            {
                if (await _desk.Payables.GetBillAsync(billId, cancellationToken) is not { } existing)
                {
                    return SaveOperationsBillResponse.Failure(OperationsPayableErrors.NotFound);
                }

                bill = existing;
                touched.AddRange(bill.Allocations.Where(allocation => allocation.DispatchId is not null).Select(allocation => allocation.DispatchId!.Value));
                bill.Change(request.Party, request.PartyId, partyName, reference, request.Category, request.BilledOn, request.DueOn, request.Amount, request.Note, allocations!,
                    access!.User.Id, utcNow);
            }
            else
            {
                bill = OperationsBill.Enter(context.RequiredTenantId, request.Party, request.PartyId, partyName, reference, request.Category, request.BilledOn, request.DueOn,
                    request.Amount, settings.CurrencyCode, request.Note, allocations!, access!.User.Id, utcNow);
                _desk.Payables.Add(bill);
            }

            // The company asks for no approval: the bill counts at once.
            if (!settings.BillsNeedApproval)
            {
                bill.Approve(access.User.Id, true, utcNow);
            }
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsBillResponse.Failure(exception.Error);
        }

        touched.AddRange(bill.Allocations.Where(allocation => allocation.DispatchId is not null).Select(allocation => allocation.DispatchId!.Value));
        await _desk.ApplyAsync(bill, touched, utcNow, cancellationToken);
        _audit.Record(access.User.Id, request.BillId is null ? "ops.bills.entered" : "ops.bills.changed", nameof(OperationsBill), bill.Id,
            after: new { party = bill.PartyName, bill.Reference, category = bill.Category.ToString(), bill.Amount, status = bill.Status.ToString(), shares = bill.Allocations.Count });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsBillResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        return SaveOperationsBillResponse.Success(bill.Id);
    }
}
