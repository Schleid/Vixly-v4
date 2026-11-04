using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCashDrawer.cs
//
// PURPOSE:
// A cashier's cash drawer at a warehouse (Slice 5F, C-4): opened with a float, cash in from payments and out for refunds, closed with a count.
//
// RESPONSIBILITIES:
// - Open with a starting float
// - Take cash in and give cash back
// - Close with the count and record the difference
//
// IMPORTANT NOTES:
// - One open drawer per person and warehouse (unique index)
// - The difference (over or short) is kept with a note, never hidden
// =========================================================
public sealed class OperationsCashDrawer : OperationsEntity
{
    private OperationsCashDrawer()
    {
    }

    private OperationsCashDrawer(Guid tenantId, Guid warehouseId, Guid userId, decimal startingFloat, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        (WarehouseId, UserId, Float, OpenedUtc, Status) = (warehouseId, userId, startingFloat, utcNow, OperationsCashDrawerStatus.Open);
    }

    public Guid WarehouseId { get; private set; }

    public Guid UserId { get; private set; }

    public OperationsCashDrawerStatus Status { get; private set; }

    public decimal Float { get; private set; }

    public decimal CashIn { get; private set; }

    public decimal CashOut { get; private set; }

    public decimal Expected => Float + CashIn - CashOut;

    public decimal? Counted { get; private set; }

    public decimal? Difference { get; private set; }

    public string? CloseNote { get; private set; }

    public DateTimeOffset OpenedUtc { get; private set; }

    public DateTimeOffset? ClosedUtc { get; private set; }

    public static OperationsCashDrawer Open(Guid tenantId, Guid warehouseId, Guid userId, decimal startingFloat, DateTimeOffset utcNow) =>
        startingFloat < 0m || decimal.Round(startingFloat, 2) != startingFloat || startingFloat > 1_000_000m
            ? throw new DomainException(OperationsPaymentErrors.AmountInvalid)
            : new OperationsCashDrawer(tenantId, warehouseId, userId, startingFloat, utcNow);

    internal void TakeIn(decimal amount, DateTimeOffset utcNow)
    {
        EnsureOpen();
        CashIn += amount;
        MarkAsModified(utcNow);
    }

    internal void GiveOut(decimal amount, DateTimeOffset utcNow)
    {
        EnsureOpen();
        CashOut += amount;
        MarkAsModified(utcNow);
    }

    public void Close(decimal counted, string? note, DateTimeOffset utcNow)
    {
        EnsureOpen();

        if (counted < 0m || decimal.Round(counted, 2) != counted)
        {
            throw new DomainException(OperationsPaymentErrors.AmountInvalid);
        }

        decimal difference = counted - Expected;
        string? why = OperationsBillingLimits.Text(note, OperationsBillingLimits.ReasonMaxLength, OperationsPaymentErrors.TextInvalid);

        // Over or short: the cashier says why.
        if (difference != 0m && why is null)
        {
            throw new DomainException(OperationsPaymentErrors.ReasonRequired);
        }

        (Counted, Difference, CloseNote, ClosedUtc, Status) = (counted, difference, why, utcNow, OperationsCashDrawerStatus.Closed);
        MarkAsModified(utcNow);
    }

    private void EnsureOpen()
    {
        if (Status != OperationsCashDrawerStatus.Open)
        {
            throw new DomainException(OperationsPaymentErrors.DrawerClosed);
        }
    }
}
