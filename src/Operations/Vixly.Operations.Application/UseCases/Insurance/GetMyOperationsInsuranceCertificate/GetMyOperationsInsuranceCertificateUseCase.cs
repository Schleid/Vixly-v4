using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.UseCases.Insurance.GetMyOperationsInsuranceCertificate;

// =========================================================
// FILE: GetMyOperationsInsuranceCertificateUseCase.cs
//
// PURPOSE:
// The insurance certificate of the customer's own shipment (Slice 5B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only the customer's own shipment (S-3)
// =========================================================
public sealed class GetMyOperationsInsuranceCertificateUseCase
{
    private readonly Vixly.Operations.Application.Customers.OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsCertificateDocuments _documents;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly OperationsCertificates _certificates;

    public GetMyOperationsInsuranceCertificateUseCase(
        Vixly.Operations.Application.Customers.OperationsCustomerAccounts accounts,
        IOperationsReceivingRepository receiving,
        OperationsCertificateDocuments documents,
        IOperationsLabelRenderer renderer,
        IOperationsUnitOfWork unitOfWork,
        OperationsCertificates certificates)
    {
        _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<GetMyOperationsInsuranceCertificateResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsInsuranceCertificateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsInsuranceCertificateResponse.Failure(Vixly.Operations.Application.Customers.OperationsCustomerAuthErrors.SessionRequired);
        }

        // Only the customer's own shipment; another customer's answers "not found" (S-3).
        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment || shipment.CustomerId != signedIn.Customer.Id)
        {
            return GetMyOperationsInsuranceCertificateResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        // A shipment insured before certificates existed (Slice 5B) gets its certificate when first asked.
        await _certificates.IssueMissingAsync(shipment, cancellationToken);

        if (await _documents.BuildAsync(shipment, cancellationToken) is not { } built)
        {
            return GetMyOperationsInsuranceCertificateResponse.Failure(OperationsInsuranceErrors.NotFound);
        }

        OperationsCertificateDocument certificate = built;

        // Saved for the number the certificate gave the shipment; two asked at once: the second builds again with it (as the shipment's documents).
        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();

            if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { Number: not null } numbered
                || await _documents.BuildAsync(numbered, cancellationToken) is not { } again)
            {
                return GetMyOperationsInsuranceCertificateResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
            }

            certificate = again;
        }

        return GetMyOperationsInsuranceCertificateResponse.Success(Render(certificate, signedIn.Customer.PreferredLanguage.Value), "insurance-certificate-" + certificate.CertificateNumber + ".pdf");
    }

    // In the customer's language, whatever the calling site's (as the shipment's documents).
    private byte[] Render(OperationsCertificateDocument certificate, string language)
    {
        System.Globalization.CultureInfo before = System.Globalization.CultureInfo.CurrentUICulture;

        try
        {
            System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(language);
            return _renderer.RenderInsuranceCertificate(certificate);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = before;
        }
    }
}
