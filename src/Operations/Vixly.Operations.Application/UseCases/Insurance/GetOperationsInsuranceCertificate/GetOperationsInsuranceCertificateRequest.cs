namespace Vixly.Operations.Application.UseCases.Insurance.GetOperationsInsuranceCertificate;

// =========================================================
// FILE: GetOperationsInsuranceCertificateRequest.cs
//
// PURPOSE:
// A shipment's insurance certificate, for staff (Slice 5B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsInsuranceCertificateRequest
{
    public Guid ShipmentId { get; init; }
}
