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

namespace Vixly.Operations.Application.UseCases.Payables.SaveOperationsVendor;

// =========================================================
// FILE: SaveOperationsVendorUseCase.cs
//
// PURPOSE:
// Adds or changes a vendor (Slice 8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bills.enter; one name per company
// =========================================================
public sealed class SaveOperationsVendorUseCase
{
    private readonly OperationsPayablesDesk _desk;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsVendorUseCase(
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

    public async Task<SaveOperationsVendorResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsVendorRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _desk.RequireAsync(context, OperationsPermissions.BillsEnter, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsVendorResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsVendor vendor;

        try
        {
            if (request.VendorId is { } vendorId)
            {
                vendor = await _desk.Payables.GetVendorAsync(vendorId, cancellationToken) ?? throw new DomainException(OperationsPayableErrors.NotFound);
                vendor.Change(request.Name, request.Category, request.Contact, request.Active, utcNow);
            }
            else
            {
                vendor = OperationsVendor.Create(context.RequiredTenantId, request.Name, request.Category, request.Contact, utcNow);
                _desk.Payables.Add(vendor);
            }
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsVendorResponse.Failure(exception.Error);
        }

        if (await _desk.Payables.VendorNameTakenAsync(vendor.Name, vendor.Id, cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsVendorResponse.Failure(OperationsPayableErrors.VendorNameTaken);
        }

        _audit.Record(access!.User.Id, request.VendorId is null ? "ops.vendors.added" : "ops.vendors.changed", nameof(OperationsVendor), vendor.Id,
            after: new { vendor.Name, category = vendor.Category.ToString(), vendor.Active });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SaveOperationsVendorResponse.Success(vendor.Id);
        }

        _unitOfWork.DiscardChanges();
        return SaveOperationsVendorResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
