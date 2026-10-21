using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.UseCases.Insurance.GetOperationsInsuranceCertificate;

// =========================================================
// FILE: GetOperationsInsuranceCertificateUseCase.cs
//
// PURPOSE:
// A shipment's insurance certificate, for staff (Slice 5B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.shipments.view for the origin warehouse
// =========================================================
public sealed class GetOperationsInsuranceCertificateUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsCertificateDocuments _documents;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly OperationsCertificates _certificates;

    public GetOperationsInsuranceCertificateUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        OperationsCertificateDocuments documents,
        IOperationsLabelRenderer renderer,
        IOperationsUnitOfWork unitOfWork,
        OperationsCertificates certificates)
    {
        _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<GetOperationsInsuranceCertificateResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsInsuranceCertificateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsInsuranceCertificateResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment
            || await _scope.CoveredAsync(access, OperationsPermissions.ShipmentsView, shipment.OriginWarehouseId, cancellationToken) is null)
        {
            return GetOperationsInsuranceCertificateResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        // A shipment insured before certificates existed (Slice 5B) gets its certificate when first asked.
        await _certificates.IssueMissingAsync(shipment, cancellationToken);

        if (await _documents.BuildAsync(shipment, cancellationToken) is not { } built)
        {
            return GetOperationsInsuranceCertificateResponse.Failure(OperationsInsuranceErrors.NotFound);
        }

        OperationsCertificateDocument certificate = built;

        // Saved for the number the certificate gave the shipment; two asked at once: the second builds again with it (as the shipment's documents).
        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();

            if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { Number: not null } numbered
                || await _documents.BuildAsync(numbered, cancellationToken) is not { } again)
            {
                return GetOperationsInsuranceCertificateResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
            }

            certificate = again;
        }

        return GetOperationsInsuranceCertificateResponse.Success(_renderer.RenderInsuranceCertificate(certificate), "insurance-certificate-" + certificate.CertificateNumber + ".pdf");
    }
}
