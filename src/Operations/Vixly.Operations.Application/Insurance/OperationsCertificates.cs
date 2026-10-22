using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.Insurance;

// =========================================================
// FILE: OperationsCertificates.cs
//
// PURPOSE:
// Keeps a shipment's insurance certificate in step with what the customer chose (Slice 5B, design section 4).
//
// RESPONSIBILITIES:
// - When the customer insures: issue a certificate under the cover of the shipment's mode and the policy that cover sits under
// - When the customer accepts the terms again: renew it under the cover and policy of that moment
// - When the customer stops insuring or cancels the shipment: void it
// - When a certificate is asked for and missing (a shipment insured before certificates existed): issue it
//
// IMPORTANT NOTES:
// - The use cases that change a shipment call SyncAsync before they save; reading a certificate calls IssueMissingAsync only, which never
//   renews or voids (review: a later change of the company's cover or policy never rewrites a certificate already issued)
// - Provisional until the shipment leaves (Slice 5 fixes its value and premium); a booked shipment keeps its certificate
// =========================================================
public sealed class OperationsCertificates
{
    private readonly IOperationsInsuranceRepository _insurance;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsNumbering _numbering;

    private readonly TimeProvider _timeProvider;

    public OperationsCertificates(IOperationsInsuranceRepository insurance, IOperationsChargeRepository charges, OperationsNumbering numbering, TimeProvider timeProvider)
    {
        _insurance = insurance ?? throw new ArgumentNullException(nameof(insurance));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task SyncAsync(OperationsShipment shipment, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        OperationsInsuranceCertificate? live = await _insurance.FindLiveCertificateAsync(shipment.Id, cancellationToken);

        // The customer no longer insures, or cancelled the shipment.
        if (shipment.Status == OperationsShipmentStatus.Cancelled || !shipment.Options.HasFlag(OperationsShipmentOptions.Insured) || shipment.InsuranceAcceptedUtc is not { } accepted)
        {
            live?.Void(utcNow);
            return;
        }

        if (live is null)
        {
            await IssueAsync(shipment, accepted, utcNow, cancellationToken);
        }
        else if (live.AcceptedUtc != accepted && await ProductAsync(shipment, cancellationToken) is { IsActive: true } plan)
        {
            // Accepted again (the customer changed the shipment): the cover and policy of that moment.
            live.Renew(plan.Id, plan.PolicyId, plan.Mode, accepted, utcNow);
        }
    }

    // Reading a certificate: issue one only when an insured, open shipment has none; never renew or void.
    public async Task IssueMissingAsync(OperationsShipment shipment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        if (shipment.Status is OperationsShipmentStatus.Open or OperationsShipmentStatus.Booked && shipment.Options.HasFlag(OperationsShipmentOptions.Insured)
            && shipment.InsuranceAcceptedUtc is { } accepted
            && await _insurance.FindLiveCertificateAsync(shipment.Id, cancellationToken) is null
            && await IssueAsync(shipment, accepted, _timeProvider.GetUtcNow(), cancellationToken))
        {
            // The shipment's row version guards against a change of the shipment at the same moment (review).
            shipment.InsuranceCertified(_timeProvider.GetUtcNow());
        }
    }

    // Slice 5D: the product the customer chose; a shipment from before products has its mode's first product.
    private async Task<OperationsInsurancePlan?> ProductAsync(OperationsShipment shipment, CancellationToken cancellationToken)
    {
        OperationsTransportMode mode = OperationsTransportModes.Of(shipment.ServiceCode);
        IReadOnlyList<OperationsInsurancePlan> products = (await _charges.ListInsurancePlansAsync(cancellationToken)).Where(plan => plan.Mode == mode).ToList();
        return shipment.InsurancePlanId is { } id
            ? products.FirstOrDefault(plan => plan.Id == id)
            : products.OrderByDescending(plan => plan.IsActive).ThenBy(plan => plan.Position).FirstOrDefault();
    }

    private async Task<bool> IssueAsync(OperationsShipment shipment, DateTimeOffset accepted, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        if (await ProductAsync(shipment, cancellationToken) is not { IsActive: true } plan)
        {
            return false;
        }

        _insurance.Add(OperationsInsuranceCertificate.Issue(shipment.TenantId, await _numbering.NextAsync(OperationsNumberKind.Certificate, cancellationToken),
            shipment.Id, shipment.CustomerId, plan.Id, plan.PolicyId, plan.Mode, accepted, utcNow));
        return true;
    }
}
