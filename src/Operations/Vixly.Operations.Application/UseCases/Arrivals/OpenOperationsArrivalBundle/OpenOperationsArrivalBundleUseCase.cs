using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.OpenOperationsArrivalBundle;

// =========================================================
// FILE: OpenOperationsArrivalBundleUseCase.cs
//
// PURPOSE:
// Open a bundle at its destination, so its parcels can be scanned (Slice 7, R-1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only a bundle that arrived here and ends here
// =========================================================
public sealed class OpenOperationsArrivalBundleUseCase
{
    private readonly OperationsArrivalDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public OpenOperationsArrivalBundleUseCase(
        OperationsArrivalDesk desk,
        IOperationsDispatchRepository dispatches,
        IOperationsUnitOfWork unitOfWork)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<OpenOperationsArrivalBundleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        OpenOperationsArrivalBundleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.ArrivalsReceive, cancellationToken);

        if (refused is not null)
        {
            return OpenOperationsArrivalBundleResponse.Failure(refused);
        }

        (OperationsDispatch? dispatch, OperationsWarehouse? here) = await _desk.DispatchAsync(request.DispatchId, request.WarehouseId, warehouses, cancellationToken);

        if (dispatch is null || here is null)
        {
            return OpenOperationsArrivalBundleResponse.Failure(OperationsArrivalErrors.NotFound);
        }

        if ((await _dispatches.ListDispatchBundlesAsync(dispatch.Id, cancellationToken)).FirstOrDefault(item => item.Id == request.BundleId) is not { } bundle
            || bundle.ArrivedUtc is null || bundle.CurrentWarehouseId != here.Id || !OperationsArrivalDesk.EndsAt(here, bundle))
        {
            return OpenOperationsArrivalBundleResponse.Failure(OperationsArrivalErrors.NotHere);
        }

        if (await _desk.OpenBundleAsync(dispatch, bundle, here, access!.User.Id, cancellationToken) is { } failed)
        {
            _unitOfWork.DiscardChanges();
            return OpenOperationsArrivalBundleResponse.Failure(failed);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return OpenOperationsArrivalBundleResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return OpenOperationsArrivalBundleResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
