using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcelPhoto;

// =========================================================
// FILE: GetOperationsParcelPhotoUseCase.cs
//
// PURPOSE:
// The file of one parcel photo, for staff (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The photo must belong to the parcel named in the address, and the parcel to a covered warehouse
// =========================================================
public sealed class GetOperationsParcelPhotoUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsFileStore _files;

    public GetOperationsParcelPhotoUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        IOperationsFileStore files)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public async Task<GetOperationsParcelPhotoResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsParcelPhotoRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsParcelPhotoResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, warehouseId, cancellationToken) is null
            || await _receiving.GetPhotoAsync(request.PhotoId, cancellationToken) is not { } photo
            || photo.ParcelId != parcel.Id
            || await _files.ReadAsync(context.RequiredTenantId, photo.StorageKey, cancellationToken) is not { } content)
        {
            return GetOperationsParcelPhotoResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        return GetOperationsParcelPhotoResponse.Success(content, photo.ContentType);
    }
}
