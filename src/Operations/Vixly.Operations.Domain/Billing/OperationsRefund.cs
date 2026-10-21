using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsRefund.cs
//
// PURPOSE:
// Money given back on an invoice after a credit note (Slice 5F, F-7 and C-8).
//
// RESPONSIBILITIES:
// - Give back at most what the credit notes took off a paid invoice
// - Cash comes out of the person's open drawer; Zelle, transfer and the others are recorded with their reference
//
// IMPORTANT NOTES:
// - Right refund payments; always with a reason
// - Numbered with the payment prefix
// =========================================================
public sealed class OperationsRefund : OperationsEntity
{
    private OperationsRefund()
    {
        Number = string.Empty;
        Reason = string.Empty;
    }

    private OperationsRefund(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Number = string.Empty;
        Reason = string.Empty;
    }

    public string Number { get; private set; }

    public Guid InvoiceId { get; private set; }

    public Guid WarehouseId { get; private set; }

    public OperationsPaymentMethod Method { get; private set; }

    public decimal Amount { get; private set; }

    public string Reason { get; private set; }

    public string? Reference { get; private set; }

    public Guid? DrawerId { get; private set; }

    public Guid ByUserId { get; private set; }

    public Guid RequestKey { get; private set; }

    public static OperationsRefund Give(Guid tenantId, OperationsInvoice invoice, string number, OperationsPaymentMethod method, decimal amount, string? reason, string? reference,
        OperationsCashDrawer? drawer, Guid byUserId, Guid requestKey, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (!Enum.IsDefined(method) || amount <= 0m || decimal.Round(amount, 2) != amount)
        {
            throw new DomainException(OperationsPaymentErrors.AmountInvalid);
        }

        if (amount > invoice.Refundable)
        {
            throw new DomainException(OperationsPaymentErrors.RefundTooHigh);
        }

        if (method == OperationsPaymentMethod.Cash && (drawer is not { Status: OperationsCashDrawerStatus.Open } || drawer.WarehouseId != invoice.WarehouseId || drawer.UserId != byUserId))
        {
            throw new DomainException(OperationsPaymentErrors.DrawerRequired);
        }

        // Never more cash than the drawer holds (review of 5F).
        if (method == OperationsPaymentMethod.Cash && amount > drawer!.Expected)
        {
            throw new DomainException(OperationsPaymentErrors.DrawerShort);
        }

        OperationsRefund refund = new(tenantId, utcNow)
        {
            Number = number,
            InvoiceId = invoice.Id,
            WarehouseId = invoice.WarehouseId,
            Method = method,
            Amount = amount,
            Reason = OperationsBillingLimits.Text(reason, OperationsBillingLimits.ReasonMaxLength, OperationsPaymentErrors.TextInvalid)
                ?? throw new DomainException(OperationsPaymentErrors.ReasonRequired),
            Reference = OperationsBillingLimits.Text(reference, 60, OperationsPaymentErrors.TextInvalid),
            DrawerId = method == OperationsPaymentMethod.Cash ? drawer!.Id : null,
            ByUserId = byUserId,
            RequestKey = requestKey
        };

        invoice.Refunded(amount, utcNow);

        if (method == OperationsPaymentMethod.Cash)
        {
            drawer!.GiveOut(amount, utcNow);
        }

        return refund;
    }
}
