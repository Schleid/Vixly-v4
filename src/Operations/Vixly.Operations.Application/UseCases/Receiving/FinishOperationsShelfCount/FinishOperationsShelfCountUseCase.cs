using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Receiving.FinishOperationsShelfCount;

// =========================================================
// FILE: FinishOperationsShelfCountUseCase.cs
//
// PURPOSE:
// Finishes a shelf count (Slice 12D, owner H-7): what was found, what is missing, what was recorded elsewhere, and unknown codes.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Finishing twice changes nothing
// - Extra: a parcel on hand here but recorded on another shelf or none; the page offers to move it here
// =========================================================
public sealed class FinishOperationsShelfCountUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsShelfCountRepository _counts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public FinishOperationsShelfCountUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsShelfCountRepository counts,
        IOperationsReceivingRepository receiving,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _counts = counts ?? throw new ArgumentNullException(nameof(counts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<FinishOperationsShelfCountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        FinishOperationsShelfCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return FinishOperationsShelfCountResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _counts.GetAsync(request.CountId, cancellationToken) is not { } count
            || await _scope.CoveredAsync(access, OperationsPermissions.ReceivingReceive, count.WarehouseId, cancellationToken) is null)
        {
            return FinishOperationsShelfCountResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        if (!count.IsOpen)
        {
            return FinishOperationsShelfCountResponse.Success();
        }

        // What the records say is on the shelf: parcels on hand at this warehouse, put away there.
        OperationsParcelFilter filter = new(new[] { count.WarehouseId }, null, false, false, true, count.LocationId);
        IReadOnlyList<OperationsParcel> expected = (await _receiving.ListParcelsAsync(filter, 1, OperationsShelfCount.MaxScans, cancellationToken)).Items;
        HashSet<string> expectedNumbers = expected.Select(parcel => parcel.Number ?? string.Empty).Where(number => number.Length > 0).ToHashSet(StringComparer.Ordinal);
        List<string> found = new(), extra = new(), unknown = new();

        foreach (string code in count.ScannedCodes)
        {
            if (expectedNumbers.Contains(code))
            {
                found.Add(code);
            }
            else if (await _receiving.FindParcelByNumberAsync(code, cancellationToken) is { WarehouseId: { } at } parcel && at == count.WarehouseId && parcel.IsAtOrigin)
            {
                extra.Add(code);
            }
            else
            {
                unknown.Add(code);
            }
        }

        List<string> missing = expectedNumbers.Where(number => !found.Contains(number)).OrderBy(number => number, StringComparer.Ordinal).ToList();
        count.Finish(found, missing, extra, unknown, access.User.Id, _timeProvider.GetUtcNow());
        _audit.Record(access.User.Id, "ops.parcels.shelf_counted", nameof(OperationsShelfCount), count.Id,
            after: new { shelf = count.LocationCode, found = found.Count, missing = missing.Count, extra = extra.Count, unknown = unknown.Count });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? FinishOperationsShelfCountResponse.Success()
            : FinishOperationsShelfCountResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
