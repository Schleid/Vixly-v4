using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Holds.CloseOperationsStockCount;

// =========================================================
// FILE: CloseOperationsStockCountUseCase.cs
//
// PURPOSE:
// Closes a count once every parcel is marked; a parcel missing needs a note and the Owner or an administrator (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.assign for the count's warehouse; with a parcel missing, the Owner or an administrator
// =========================================================
public sealed class CloseOperationsStockCountUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsStockCountRepository _counts;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsTrackingRepository _tracking;

    public CloseOperationsStockCountUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsStockCountRepository counts,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOperationsTrackingRepository tracking)
    {
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _counts = counts ?? throw new ArgumentNullException(nameof(counts));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CloseOperationsStockCountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CloseOperationsStockCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return CloseOperationsStockCountResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _counts.GetAsync(request.CountId, cancellationToken) is not { } count
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsAssign, count.WarehouseId, cancellationToken) is null)
        {
            return CloseOperationsStockCountResponse.Failure(OperationsHoldErrors.CountNotFound);
        }

        // Closed already (a double click): the same answer.
        if (!count.IsOpen)
        {
            return CloseOperationsStockCountResponse.Success();
        }

        // A parcel given to its customer or decided meanwhile is no longer expected in the area (review of 4D step 3).
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        foreach (OperationsCountLine line in count.Lines.Where(line => line.Result != OperationsCountResult.Found).ToList())
        {
            if (await _tracking.GetParcelAsync(line.ParcelId, cancellationToken) is not { } parcel || parcel.IsIdentified || !parcel.IsAtOrigin)
            {
                count.Resolve(line.ParcelId, utcNow);
            }
        }

        try
        {
            count.Close(access.User.Id, access.IsOwner || access.IsAdministrator, utcNow);
        }
        catch (DomainException exception)
        {
            return CloseOperationsStockCountResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.holds.count_closed", nameof(OperationsStockCount), count.Id,
            after: new { found = count.Lines.Count(line => line.Result == OperationsCountResult.Found), missing = count.Lines.Where(line => line.Result == OperationsCountResult.Missing).Select(line => line.ParcelNumber).ToList() });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CloseOperationsStockCountResponse.Success()
            : CloseOperationsStockCountResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
