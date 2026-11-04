using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCardReader.cs
//
// PURPOSE:
// A Stripe Terminal card reader at one of the company's warehouses (Slice 5F step 4, owner 2026-10-21).
//
// RESPONSIBILITIES:
// - Name the reader and the warehouse it sits at, with Stripe's reader id
//
// IMPORTANT NOTES:
// - The company registers the reader with Stripe first; Vixly only keeps its id (tmr_...)
// - Removed readers are deleted; payments keep their own record
// =========================================================
public sealed class OperationsCardReader : OperationsEntity
{
    public const int LabelMaxLength = 60;

    private OperationsCardReader()
    {
        Label = string.Empty;
        StripeReaderId = string.Empty;
    }

    private OperationsCardReader(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Label = string.Empty;
        StripeReaderId = string.Empty;
    }

    public Guid WarehouseId { get; private set; }

    public string Label { get; private set; }

    public string StripeReaderId { get; private set; }

    public static OperationsCardReader Add(Guid tenantId, Guid warehouseId, string? label, string? stripeReaderId, DateTimeOffset utcNow)
    {
        string name = (label ?? string.Empty).Trim();
        string id = (stripeReaderId ?? string.Empty).Trim();

        if (warehouseId == Guid.Empty || name.Length is 0 or > LabelMaxLength || name.Any(char.IsControl)
            || !id.StartsWith("tmr_", StringComparison.Ordinal) || id.Length is < 8 or > 64 || !id.All(letter => char.IsAsciiLetterOrDigit(letter) || letter == '_'))
        {
            throw new DomainException(OperationsCardErrors.ReaderInvalid);
        }

        return new OperationsCardReader(tenantId, utcNow) { WarehouseId = warehouseId, Label = name, StripeReaderId = id };
    }
}
