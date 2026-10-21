using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.Insurance;

// =========================================================
// FILE: OperationsCertificateDocuments.cs
//
// PURPOSE:
// Builds a shipment's insurance certificate for printing (Slice 5B, design section 4).
//
// RESPONSIBILITIES:
// - The live certificate, its cover's terms in force when the terms were accepted, the policy it sits under
// - The value declared today (parcels received and packages still announced) and the premium it gives
// - Number the shipment with the certificate, as with the shipment's other documents
//
// IMPORTANT NOTES:
// - Null when the shipment has no live certificate; the caller checks who may see it
// =========================================================
public sealed class OperationsCertificateDocuments
{
    private readonly IOperationsInsuranceRepository _insurance;

    private readonly IOperationsChargeRepository _charges;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsBrandRepository _brands;

    private readonly OperationsNumbering _numbering;

    private readonly TimeProvider _timeProvider;

    public OperationsCertificateDocuments(IOperationsInsuranceRepository insurance, IOperationsChargeRepository charges, IOperationsReceivingRepository receiving,
        IOperationsCustomerRepository customers, IOperationsLocationRepository locations, IOperationsSettingsRepository settings, IOperationsBrandRepository brands,
        OperationsNumbering numbering, TimeProvider timeProvider)
    {
        _insurance = insurance ?? throw new ArgumentNullException(nameof(insurance));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<OperationsCertificateDocument?> BuildAsync(OperationsShipment shipment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        if ((await _insurance.FindLiveCertificateAsync(shipment.Id, cancellationToken) ?? await _insurance.FindLatestCertificateAsync(shipment.Id, cancellationToken)) is not { } certificate
            || await _charges.GetInsurancePlanAsync(certificate.PlanId, cancellationToken) is not { } plan
            || (plan.TermsAt(certificate.AcceptedUtc) ?? plan.TermsAt(certificate.CreatedUtc)) is not { } terms
            || await _customers.GetAsync(shipment.CustomerId, cancellationToken) is not { } customer)
        {
            return null;
        }

        if (shipment.Number is null)
        {
            shipment.Numbered(await _numbering.NextAsync(OperationsNumberKind.Shipment, cancellationToken));
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        OperationsInsurancePolicy? policy = certificate.PolicyId is { } policyId ? await _insurance.GetPolicyAsync(policyId, cancellationToken) : null;

        // The value insured today: what arrived (still travelling) and what is still announced.
        List<decimal> items = (await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken))
            .Where(parcel => parcel.Milestone is null or < Vixly.Operations.Domain.Tracking.OperationsMilestone.Cancelled).Select(parcel => parcel.DeclaredValue ?? 0m)
            .Concat((await _receiving.ListShipmentPreAlertsAsync(shipment.Id, cancellationToken)).Where(preAlert => preAlert.Status == OperationsPreAlertStatus.Open).Select(preAlert => preAlert.DeclaredValue))
            .ToList();
        decimal declared = certificate.DeclaredValue ?? items.Sum();
        IReadOnlyList<OperationsWarehouse> warehouses = await _locations.ListWarehousesAsync(cancellationToken);
        OperationsWarehouse? origin = warehouses.FirstOrDefault(item => item.Id == shipment.OriginWarehouseId);
        OperationsWarehouse? destination = shipment.DestinationWarehouseId is { } destinationId ? warehouses.FirstOrDefault(item => item.Id == destinationId) : null;
        string city = shipment.DeliveryAddress?.City ?? destination?.Address.City ?? string.Empty;
        OperationsInsuranceCoverage coverage = terms.Coverage;

        return new OperationsCertificateDocument(
            (await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty,
            certificate.Number,
            certificate.Status,
            DateOnly.FromDateTime(certificate.CreatedUtc.UtcDateTime),
            certificate.AcceptedUtc,
            shipment.Number!,
            customer.FullName + (customer.Code is { } code ? " (" + code + ")" : string.Empty),
            shipment.RecipientName ?? string.Empty,
            (origin?.Code ?? string.Empty) + " > " + (destination?.Code ?? shipment.DestinationCountryCode ?? string.Empty) + (city.Length > 0 ? " (" + city + ")" : string.Empty),
            certificate.Mode,
            shipment.ServiceCode,
            policy?.InsurerName ?? (coverage.Cover == OperationsInsuranceCover.Insurer ? coverage.InsurerName : null),
            policy?.PolicyNumber ?? (coverage.Cover == OperationsInsuranceCover.Insurer ? coverage.PolicyNumber : null),
            policy is null ? coverage.Cover == OperationsInsuranceCover.Own : policy.Kind == OperationsPolicyKind.SelfInsured,
            coverage.Risks,
            coverage.Exclusions,
            coverage.OtherExclusions,
            coverage.Payout,
            coverage.DeductibleAmount,
            coverage.DeductiblePercent,
            terms.MaxValue,
            declared,
            // The same rule as pricing: no premium when one item is worth more than the most paid per item, or the whole above the maximum.
            certificate.Premium ?? terms.Premium(declared, items),
            certificate.CurrencyCode ?? settings?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency);
    }
}
