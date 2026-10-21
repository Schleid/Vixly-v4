namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsDispatchDocuments.cs
//
// PURPOSE:
// The documents that travel with an air dispatch, as the Shipper's Letter of Instruction lists them (Slice 6B step 2, D-6; as V3).
//
// RESPONSIBILITIES:
// - Name each document
//
// IMPORTANT NOTES:
// - Stored as flags; never renumber
// =========================================================
[Flags]
public enum OperationsDispatchDocuments
{
    None = 0,
    CommercialInvoice = 1,
    PackingList = 2,
    ExportDeclaration = 4,
    CertificateOfOrigin = 8,
    ExportLicence = 16,
    DangerousGoodsDeclaration = 32
}
