using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Shipments.GetOperationsShipmentDocument;

// =========================================================
// FILE: GetOperationsShipmentDocumentUseCase.cs
//
// PURPOSE:
// The packing list or the pro forma commercial invoice of a shipment, for staff (Slice 5A).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.shipments.view for the origin warehouse
// - Numbers the shipment with its first document
// =========================================================
public sealed class GetOperationsShipmentDocumentUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsShipmentDocuments _documents;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public GetOperationsShipmentDocumentUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        OperationsShipmentDocuments documents,
        IOperationsLabelRenderer renderer,
        IOperationsUnitOfWork unitOfWork)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<GetOperationsShipmentDocumentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsShipmentDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsShipmentDocumentResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment
            || await _scope.CoveredAsync(access, OperationsPermissions.ShipmentsView, shipment.OriginWarehouseId, cancellationToken) is null)
        {
            return GetOperationsShipmentDocumentResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        if (!Enum.IsDefined(request.Kind) || await _documents.BuildAsync(shipment, request.Kind, cancellationToken) is not { } built)
        {
            return GetOperationsShipmentDocumentResponse.Failure(OperationsShipmentErrors.NoDocuments);
        }

        OperationsShipmentDocument document = built;

        // Saved for the number the first document gave the shipment. Two documents asked at once both number it: the second
        // reloads the shipment, which has its number now, and builds again (review).
        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();

            if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { Number: not null } numbered
                || await _documents.BuildAsync(numbered, request.Kind, cancellationToken) is not { } again)
            {
                return GetOperationsShipmentDocumentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
            }

            document = again;
        }

        string file = (request.Kind == OperationsShipmentDocumentKind.PackingList ? "packing-list-" : "commercial-invoice-") + document.ShipmentNumber + ".pdf";
        return GetOperationsShipmentDocumentResponse.Success(_renderer.RenderShipmentDocument(document), file);
    }
}
