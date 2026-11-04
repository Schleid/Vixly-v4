using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsInsuranceCertificate.cs
//
// PURPOSE:
// The proof that a customer insured a shipment: its number, the cover and policy it sits under, when the terms were accepted (Slice 5B, design section 4).
//
// RESPONSIBILITIES:
// - Issue a certificate when the customer insures; void it when the customer drops the cover or the shipment is cancelled
// - Keep the declared value and premium once final
//
// IMPORTANT NOTES:
// - One live certificate per shipment
// - While provisional, the value and premium shown are those of the shipment today; they are fixed when it leaves (Slice 5)
// =========================================================
public sealed class OperationsInsuranceCertificate : OperationsEntity
{
    private OperationsInsuranceCertificate()
    {
        Number = string.Empty;
    }

    private OperationsInsuranceCertificate(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Number = string.Empty;
    }

    public string Number { get; private set; }

    public Guid ShipmentId { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid PlanId { get; private set; }

    public Guid? PolicyId { get; private set; }

    public Vixly.Operations.Domain.Charges.OperationsTransportMode Mode { get; private set; }

    public DateTimeOffset AcceptedUtc { get; private set; }

    public OperationsCertificateStatus Status { get; private set; }

    public decimal? DeclaredValue { get; private set; }

    public decimal? Premium { get; private set; }

    public string? CurrencyCode { get; private set; }

    public DateTimeOffset? VoidedUtc { get; private set; }

    public bool IsLive => Status != OperationsCertificateStatus.Void;

    public static OperationsInsuranceCertificate Issue(Guid tenantId, string number, Guid shipmentId, Guid customerId, Guid planId, Guid? policyId,
        Vixly.Operations.Domain.Charges.OperationsTransportMode mode, DateTimeOffset acceptedUtc, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        return new OperationsInsuranceCertificate(tenantId, utcNow)
        {
            Number = number, ShipmentId = shipmentId, CustomerId = customerId, PlanId = planId, PolicyId = policyId, Mode = mode, AcceptedUtc = acceptedUtc,
            Status = OperationsCertificateStatus.Provisional
        };
    }

    // The customer accepted the terms again (a change of the shipment): the cover and policy in force now.
    public void Renew(Guid planId, Guid? policyId, Vixly.Operations.Domain.Charges.OperationsTransportMode mode, DateTimeOffset acceptedUtc, DateTimeOffset utcNow)
    {
        if (Status == OperationsCertificateStatus.Provisional)
        {
            (PlanId, PolicyId, Mode, AcceptedUtc) = (planId, policyId, mode, acceptedUtc);
            MarkAsModified(utcNow);
        }
    }

    public void Void(DateTimeOffset utcNow)
    {
        if (Status == OperationsCertificateStatus.Provisional)
        {
            (Status, VoidedUtc) = (OperationsCertificateStatus.Void, utcNow);
            MarkAsModified(utcNow);
        }
    }

    public void MakeFinal(decimal declaredValue, decimal premium, string currencyCode, DateTimeOffset utcNow)
    {
        if (Status == OperationsCertificateStatus.Provisional)
        {
            (Status, DeclaredValue, Premium, CurrencyCode) = (OperationsCertificateStatus.Final, declaredValue, premium, currencyCode);
            MarkAsModified(utcNow);
        }
    }
}
