using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Holds.MarkOperationsStockCount;

// =========================================================
// FILE: MarkOperationsStockCountUseCase.cs
//
// PURPOSE:
// Marks a parcel of a count found on its shelf (picked or scanned) or missing, with a note (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.assign for the count's warehouse
// - Marking twice keeps the last mark
// =========================================================
public sealed class MarkOperationsStockCountUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsStockCountRepository _counts;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly OperationsAuditTrail _audit;

    public MarkOperationsStockCountUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsStockCountRepository counts,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        OperationsAuditTrail audit)
    {
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _counts = counts ?? throw new ArgumentNullException(nameof(counts));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<MarkOperationsStockCountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        MarkOperationsStockCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return MarkOperationsStockCountResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _counts.GetAsync(request.CountId, cancellationToken) is not { } count
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsAssign, count.WarehouseId, cancellationToken) is null)
        {
            return MarkOperationsStockCountResponse.Failure(OperationsHoldErrors.CountNotFound);
        }

        // Picked from the list, or its ID label scanned (as the inspection station reads it).
        string? scanned = Vixly.Operations.Application.Printing.OperationsZplLabels.Clean(request.Code ?? string.Empty) is { Length: > 0 } code ? code.Replace(" ", string.Empty, StringComparison.Ordinal) : null;
        Guid? parcelId = request.ParcelId ?? count.Lines.FirstOrDefault(line => scanned is not null && (line.ParcelNumber == scanned || scanned.EndsWith(line.ParcelNumber, StringComparison.Ordinal)))?.ParcelId;

        if (parcelId is not { } chosen)
        {
            return MarkOperationsStockCountResponse.Failure(OperationsHoldErrors.CountNotFound);
        }

        try
        {
            count.Mark(chosen, request.Result, request.Note, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return MarkOperationsStockCountResponse.Failure(exception.Error);
        }

        // Who marked what, and when (review of 4D step 3).
        _audit.Record(access.User.Id, "ops.holds.count_marked", nameof(OperationsStockCount), count.Id,
            after: new { parcel = count.Lines.First(line => line.ParcelId == chosen).ParcelNumber, result = request.Result.ToString(), note = request.Note });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? MarkOperationsStockCountResponse.Success()
            : MarkOperationsStockCountResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
