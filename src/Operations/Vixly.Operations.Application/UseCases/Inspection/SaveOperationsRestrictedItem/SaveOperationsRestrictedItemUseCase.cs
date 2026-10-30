using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.SaveOperationsRestrictedItem;

// =========================================================
// FILE: SaveOperationsRestrictedItemUseCase.cs
//
// PURPOSE:
// Adds an entry to the company's restricted-items list, or changes one (Slice 3C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Company-wide (ops.inspection.configure); never deleted, taken out of use instead
// =========================================================
public sealed class SaveOperationsRestrictedItemUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsRestrictedItemUseCase(
        OperationsAccess access,
        IOperationsInspectionRepository inspections,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsRestrictedItemResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsRestrictedItemRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InspectionConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsRestrictedItemResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsRestrictedItem item;

        try
        {
            if (request.ItemId is { } itemId)
            {
                if (await _inspections.GetItemAsync(itemId, cancellationToken) is not { } found)
                {
                    return SaveOperationsRestrictedItemResponse.Failure(OperationsInspectionErrors.ItemNotFound);
                }

                item = found;
                item.Change(request.Name, request.Words, request.Kind, request.Countries, request.Carriers, request.Requirement, request.Active, utcNow);

                if (await _inspections.FindItemByNameAsync(item.Name, cancellationToken) is { } other && other.Id != item.Id)
                {
                    return SaveOperationsRestrictedItemResponse.Failure(OperationsInspectionErrors.ItemDuplicate);
                }
            }
            else
            {
                if ((await _inspections.ListItemsAsync(cancellationToken)).Count >= OperationsInspectionLimits.MaxItems)
                {
                    return SaveOperationsRestrictedItemResponse.Failure(OperationsInspectionErrors.TooMany);
                }

                item = OperationsRestrictedItem.Create(context.RequiredTenantId, request.Name, request.Words, request.Kind, request.Countries, request.Carriers, request.Requirement, utcNow);

                if (await _inspections.FindItemByNameAsync(item.Name, cancellationToken) is { } named)
                {
                    return Same(named, item) ? SaveOperationsRestrictedItemResponse.Success(named.Id) : SaveOperationsRestrictedItemResponse.Failure(OperationsInspectionErrors.ItemDuplicate);
                }

                _inspections.Add(item);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsRestrictedItemResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.inspection.item_saved", nameof(OperationsRestrictedItem), item.Id,
            after: new { name = item.Name, words = item.Words, kind = item.Kind.ToString(), countries = item.Countries, carriers = item.Carriers, active = item.IsActive });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SaveOperationsRestrictedItemResponse.Success(item.Id);
        }

        // The same form sent twice at once: the other one added it (ADR-0019).
        _unitOfWork.DiscardChanges();
        return request.ItemId is null && await _inspections.FindItemByNameAsync(item.Name, cancellationToken) is { } raced && Same(raced, item)
            ? SaveOperationsRestrictedItemResponse.Success(raced.Id)
            : SaveOperationsRestrictedItemResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private static bool Same(OperationsRestrictedItem saved, OperationsRestrictedItem typed) =>
        saved.Words == typed.Words && saved.Kind == typed.Kind && saved.Countries == typed.Countries && saved.Carriers == typed.Carriers && saved.Requirement == typed.Requirement;
}
