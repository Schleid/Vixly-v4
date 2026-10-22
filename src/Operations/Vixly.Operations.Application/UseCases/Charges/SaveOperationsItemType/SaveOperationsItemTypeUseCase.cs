using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsItemType;

// =========================================================
// FILE: SaveOperationsItemTypeUseCase.cs
//
// PURPOSE:
// Adds one of the company's item types or changes one: a barrel, a car, a TV (Slice 4B design section 7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SaveOperationsItemTypeUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsItemTypeUseCase(
        OperationsAccess access,
        IOperationsChargeRepository charges,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsItemTypeResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsItemTypeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsItemTypeResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        IReadOnlyList<OperationsItemType> all = await _charges.ListItemTypesAsync(cancellationToken);
        OperationsItemType type;

        try
        {
            if (request.ItemTypeId is { } typeId)
            {
                type = await _charges.GetItemTypeAsync(typeId, cancellationToken) ?? throw new DomainException(OperationsChargeErrors.ItemTypeNotFound);
                type.Change(request.Name, request.Words, request.Active, utcNow);
            }
            else
            {
                if (all.Count >= OperationsChargeLimits.MaxItemTypes)
                {
                    return SaveOperationsItemTypeResponse.Failure(OperationsChargeErrors.TooMany);
                }

                type = OperationsItemType.Create(context.RequiredTenantId, request.Name, request.Words, utcNow);

                // The same new type sent twice: the one already there.
                if (all.FirstOrDefault(other => string.Equals(other.Name, type.Name, StringComparison.OrdinalIgnoreCase)) is { } existing)
                {
                    return existing.Words == type.Words ? SaveOperationsItemTypeResponse.Success(existing.Id) : SaveOperationsItemTypeResponse.Failure(OperationsChargeErrors.ItemTypeDuplicate);
                }

                _charges.Add(type);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsItemTypeResponse.Failure(exception.Error);
        }

        if (all.Any(other => other.Id != type.Id && string.Equals(other.Name, type.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return SaveOperationsItemTypeResponse.Failure(OperationsChargeErrors.ItemTypeDuplicate);
        }

        _audit.Record(access!.User.Id, "ops.charges.item_type_saved", nameof(OperationsItemType), type.Id, after: new { name = type.Name, words = type.Words, active = type.IsActive });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SaveOperationsItemTypeResponse.Success(type.Id);
        }

        _unitOfWork.DiscardChanges();
        return request.ItemTypeId is null && (await _charges.ListItemTypesAsync(cancellationToken)).FirstOrDefault(other => other.Name == type.Name) is { } raced
            ? SaveOperationsItemTypeResponse.Success(raced.Id)
            : SaveOperationsItemTypeResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
