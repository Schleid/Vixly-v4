using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.RemoveFromOperationsBundleList;

// =========================================================
// FILE: RemoveFromOperationsBundleListUseCase.cs
//
// PURPOSE:
// Take a line off a bundle's list before it is scanned (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage at the bundle's warehouse
// - A scanned line is taken out of the bundle first
// =========================================================
public sealed class RemoveFromOperationsBundleListUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RemoveFromOperationsBundleListUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RemoveFromOperationsBundleListResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RemoveFromOperationsBundleListRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage }, cancellationToken);

        if (refused is not null)
        {
            return RemoveFromOperationsBundleListResponse.Failure(refused);
        }

        if (await _bundles.GetBundleAsync(request.BundleId, cancellationToken) is not { } bundle || warehouses.All(item => item.Id != bundle.WarehouseId))
        {
            return RemoveFromOperationsBundleListResponse.Failure(OperationsBundleErrors.NotFound);
        }

        if (!bundle.IsOpen)
        {
            return RemoveFromOperationsBundleListResponse.Failure(OperationsBundleErrors.NotOpen);
        }

        if ((await _bundles.ListItemsAsync(bundle.Id, cancellationToken)).FirstOrDefault(item => item.Id == request.ItemId) is not { } line)
        {
            return RemoveFromOperationsBundleListResponse.Success();
        }

        try
        {
            line.CheckRemovable();
        }
        catch (DomainException exception)
        {
            return RemoveFromOperationsBundleListResponse.Failure(exception.Error);
        }

        _bundles.Remove(line);
        bundle.ListChanged(_timeProvider.GetUtcNow());
        _audit.Record(access!.User.Id, "ops.bundles.unlisted", nameof(OperationsBundle), bundle.Id, after: new { bundle.Number, parcel = line.ParcelId, box = line.BoxId });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RemoveFromOperationsBundleListResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return RemoveFromOperationsBundleListResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
