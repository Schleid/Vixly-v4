using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyBarcode.cs
//
// PURPOSE:
// One barcode of a supply (Slice 5E, S-1): a supplier's code or the company's own label.
//
// RESPONSIBILITIES:
// - Carry the code a scan sends, for the supply it belongs to
//
// IMPORTANT NOTES:
// - Unique in the company (the database refuses a second one); kept in capitals
// =========================================================
public sealed class OperationsSupplyBarcode : OperationsEntity
{
    private OperationsSupplyBarcode()
    {
        Code = string.Empty;
    }

    private OperationsSupplyBarcode(Guid tenantId, Guid supplyId, string code, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        SupplyId = supplyId;
        Code = code;
    }

    public Guid SupplyId { get; private set; }

    public string Code { get; private set; }

    internal static OperationsSupplyBarcode Create(Guid tenantId, Guid supplyId, string code, DateTimeOffset utcNow) => new(tenantId, supplyId, code, utcNow);
}
