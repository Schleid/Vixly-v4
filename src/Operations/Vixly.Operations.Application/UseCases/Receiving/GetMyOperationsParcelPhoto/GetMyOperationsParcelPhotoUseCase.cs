using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetMyOperationsParcelPhoto;

// =========================================================
// FILE: GetMyOperationsParcelPhotoUseCase.cs
//
// PURPOSE:
// The file of a photo of a customer's own package (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Ownership of the parcel and of the photo is checked on every read (S-3)
// =========================================================
public sealed class GetMyOperationsParcelPhotoUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsFileStore _files;

    public GetMyOperationsParcelPhotoUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        IOperationsFileStore files)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public async Task<GetMyOperationsParcelPhotoResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsParcelPhotoRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsParcelPhotoResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { } parcel
            || parcel.CustomerId != signedIn.Customer.Id
            || await _receiving.GetPhotoAsync(request.PhotoId, cancellationToken) is not { } photo
            || photo.ParcelId != parcel.Id
            || await _files.ReadAsync(context.RequiredTenantId, photo.StorageKey, cancellationToken) is not { } content)
        {
            return GetMyOperationsParcelPhotoResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        return GetMyOperationsParcelPhotoResponse.Success(content, photo.ContentType);
    }
}
