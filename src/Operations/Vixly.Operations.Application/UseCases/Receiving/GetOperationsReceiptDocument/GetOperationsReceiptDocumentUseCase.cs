using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsReceiptDocument;

// =========================================================
// FILE: GetOperationsReceiptDocumentUseCase.cs
//
// PURPOSE:
// The warehouse receipt of a delivery as a PDF (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Customer codes only, never names or phones: the driver signs it
// =========================================================
public sealed class GetOperationsReceiptDocumentUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsLabelRenderer _renderer;

    public GetOperationsReceiptDocumentUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsCustomerRepository customers,
        IOperationsUserRepository users,
        IOperationsBrandRepository brands,
        IOperationsLabelRenderer renderer)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    public async Task<GetOperationsReceiptDocumentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsReceiptDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsReceiptDocumentResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _receiving.GetReceiptAsync(request.ReceiptId, cancellationToken) is not { } receipt
            || await _scope.CoveredAsync(access, OperationsPermissions.ReceivingReceive, receipt.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return GetOperationsReceiptDocumentResponse.Failure(OperationsReceivingErrors.DeliveryNotFound);
        }

        IReadOnlyList<OperationsParcel> parcels = await _receiving.ListDeliveryParcelsAsync(receipt.DeliveryId, cancellationToken);
        Dictionary<Guid, string?> codes = (await _customers.ListByIdsAsync(parcels.Where(parcel => parcel.CustomerId is not null).Select(parcel => parcel.CustomerId!.Value).Distinct().ToList(), cancellationToken))
            .ToDictionary(customer => customer.Id, customer => customer.Code);
        string clerk = (await _users.GetAsync(receipt.ReceivedByUserId, cancellationToken))?.FullName ?? string.Empty;
        OperationsReceiptDocument document = new(
            (await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty,
            warehouse.Code,
            warehouse.Name,
            receipt.Number,
            receipt.Carrier,
            receipt.OpenedUtc,
            receipt.CreatedUtc,
            clerk,
            receipt.PieceCount,
            receipt.DamagedCount,
            parcels.Select(parcel => new OperationsReceiptLine(parcel.Number ?? string.Empty, parcel.CarrierTrackingNumber,
                parcel.CustomerId is { } id ? codes.GetValueOrDefault(id) : null, parcel.ArrivedDamaged)).ToList());

        return GetOperationsReceiptDocumentResponse.Success(_renderer.RenderReceipt(document), receipt.Number + ".pdf");
    }
}
