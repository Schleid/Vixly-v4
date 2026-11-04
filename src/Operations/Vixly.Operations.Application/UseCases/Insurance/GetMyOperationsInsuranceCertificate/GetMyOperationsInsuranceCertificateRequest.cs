namespace Vixly.Operations.Application.UseCases.Insurance.GetMyOperationsInsuranceCertificate;

// =========================================================
// FILE: GetMyOperationsInsuranceCertificateRequest.cs
//
// PURPOSE:
// The insurance certificate of the customer's own shipment (Slice 5B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsInsuranceCertificateRequest
{
    public string SessionToken { get; init; } = string.Empty;

    public Guid ShipmentId { get; init; }
}
