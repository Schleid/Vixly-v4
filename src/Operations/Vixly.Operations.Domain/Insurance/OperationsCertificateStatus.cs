namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsCertificateStatus.cs
//
// PURPOSE:
// Where an insurance certificate stands (Slice 5B).
//
// RESPONSIBILITIES:
// - Provisional while the shipment fills, final once it leaves (Slice 5), void when the cover is dropped
//
// IMPORTANT NOTES:
// - Values are fixed only when final
// =========================================================
public enum OperationsCertificateStatus
{
    Provisional = 1,

    Final = 2,

    Void = 3
}
