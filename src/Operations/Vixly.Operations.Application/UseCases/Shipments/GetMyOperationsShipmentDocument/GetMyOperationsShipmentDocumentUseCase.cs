using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipmentDocument;

// =========================================================
// FILE: GetMyOperationsShipmentDocumentUseCase.cs
//
// PURPOSE:
// The packing list or the pro forma commercial invoice of a shipment, for the customer (Slice 5A).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only the customer's own shipment (S-3)
// - Numbers the shipment with its first document
// =========================================================
public sealed class GetMyOperationsShipmentDocumentUseCase
{
    private readonly Vixly.Operations.Application.Customers.OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsShipmentDocuments _documents;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public GetMyOperationsShipmentDocumentUseCase(
        Vixly.Operations.Application.Customers.OperationsCustomerAccounts accounts,
        IOperationsReceivingRepository receiving,
        OperationsShipmentDocuments documents,
        IOperationsLabelRenderer renderer,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<GetMyOperationsShipmentDocumentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsShipmentDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsShipmentDocumentResponse.Failure(Vixly.Operations.Application.Customers.OperationsCustomerAuthErrors.SessionRequired);
        }

        // Only the customer's own shipment; another customer's answers "not found" (S-3).
        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment || shipment.CustomerId != signedIn.Customer.Id)
        {
            return GetMyOperationsShipmentDocumentResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        if (!Enum.IsDefined(request.Kind) || await _documents.BuildAsync(shipment, request.Kind, cancellationToken) is not { } built)
        {
            return GetMyOperationsShipmentDocumentResponse.Failure(OperationsShipmentErrors.NoDocuments);
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
                return GetMyOperationsShipmentDocumentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
            }

            document = again;
        }

        string file = (request.Kind == OperationsShipmentDocumentKind.PackingList ? "packing-list-" : "commercial-invoice-") + document.ShipmentNumber + ".pdf";
        return GetMyOperationsShipmentDocumentResponse.Success(Render(document, signedIn.Customer.PreferredLanguage.Value), file);
    }

    // In the customer's language, whatever the calling site's (review).
    private byte[] Render(OperationsShipmentDocument document, string language)
    {
        System.Globalization.CultureInfo before = System.Globalization.CultureInfo.CurrentUICulture;

        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(language);
            return _renderer.RenderShipmentDocument(document);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = before;
        }
    }
}
