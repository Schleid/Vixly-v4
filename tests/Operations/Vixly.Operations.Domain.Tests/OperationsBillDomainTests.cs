using Vixly.Operations.Domain.Payables;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsBillDomainTests.cs
//
// PURPOSE:
// Proves the payables rules (Slice 8).
//
// RESPONSIBILITIES:
// - A bill entered by one person and approved by another; sent back with a reason and corrected
// - Shares never above the bill; paid up to its amount; voided only while nothing is paid
//
// IMPORTANT NOTES:
// - Pure domain; no database
// =========================================================
public sealed class OperationsBillDomainTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Clerk = Guid.NewGuid();

    private static readonly Guid Finance = Guid.NewGuid();

    private static readonly DateTimeOffset Now = new(2026, 10, 22, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly Today = new(2026, 10, 22);

    private static OperationsBill Bill(decimal amount, params OperationsBillAllocation[] shares) =>
        OperationsBill.Enter(Tenant, OperationsBillParty.Carrier, Guid.NewGuid(), "Sky Cargo", "SC-1", OperationsBillCategory.Freight, Today, Today.AddDays(30), amount, "USD", null,
            shares, Clerk, Now);

    [Fact]
    public void Bill_IsApprovedByAnother_RejectedWithAReason_AndCorrectedBeforeApproval()
    {
        Assert.Equal(OperationsPayableErrors.AllocationTooHigh, Assert.Throws<DomainException>(() =>
            Bill(100m, new OperationsBillAllocation(Guid.NewGuid(), null, "DSP1", 80m), new OperationsBillAllocation(null, Guid.NewGuid(), "SHP1", 30m))).Error);
        Assert.Equal(OperationsPayableErrors.DatesInvalid, Assert.Throws<DomainException>(() =>
            OperationsBill.Enter(Tenant, OperationsBillParty.Vendor, Guid.NewGuid(), "Landlord", "R-1", OperationsBillCategory.Rent, Today, Today.AddDays(-1), 10m, "USD", null,
                Array.Empty<OperationsBillAllocation>(), Clerk, Now)).Error);

        OperationsBill bill = Bill(100m, new OperationsBillAllocation(Guid.NewGuid(), null, "DSP1", 80m));
        Assert.False(bill.Counts);
        Assert.Equal(OperationsPayableErrors.OwnBill, Assert.Throws<DomainException>(() => bill.Approve(Clerk, false, Now)).Error);
        Assert.Equal(OperationsPayableErrors.ReasonRequired, Assert.Throws<DomainException>(() => bill.Reject(Finance, " ", Now)).Error);
        bill.Reject(Finance, "Wrong amount", Now);
        Assert.Equal(OperationsBillStatus.Rejected, bill.Status);
        Assert.Equal(OperationsPayableErrors.NotPayable, Assert.Throws<DomainException>(() => bill.Pay(Today, 10m, OperationsBillPaymentMethod.Cheque, null, Finance, Today, Now)).Error);

        bill.Change(OperationsBillParty.Carrier, Guid.NewGuid(), "Sky Cargo", "SC-1", OperationsBillCategory.Freight, Today, Today.AddDays(30), 90m, null,
            new[] { new OperationsBillAllocation(Guid.NewGuid(), null, "DSP1", 90m) }, Clerk, Now);
        Assert.Equal((OperationsBillStatus.Entered, (string?)null), (bill.Status, bill.RejectReason));
        bill.Approve(Finance, false, Now);
        Assert.True(bill.Counts);
        Assert.Equal(OperationsPayableErrors.NotEditable, Assert.Throws<DomainException>(() => bill.Change(OperationsBillParty.Carrier, Guid.NewGuid(), "Sky Cargo", "SC-1",
            OperationsBillCategory.Freight, Today, Today, 90m, null, Array.Empty<OperationsBillAllocation>(), Clerk, Now)).Error);
    }

    [Fact]
    public void Bill_IsPaidUpToItsAmount_AndVoidedOnlyWhileNothingIsPaid()
    {
        OperationsBill bill = Bill(100m);
        bill.Approve(Clerk, true, Now);
        Assert.Equal(100m, bill.Owed);
        Assert.Equal(OperationsPayableErrors.PaymentTooHigh, Assert.Throws<DomainException>(() => bill.Pay(Today, 100.01m, OperationsBillPaymentMethod.BankTransfer, null, Finance, Today, Now)).Error);
        bill.Pay(Today, 40m, OperationsBillPaymentMethod.BankTransfer, "TRF-1", Finance, Today, Now);
        Assert.Equal((OperationsBillStatus.Approved, 60m), (bill.Status, bill.Owed));
        Assert.Equal(OperationsPayableErrors.NotVoidable, Assert.Throws<DomainException>(() => bill.Void("Duplicate", Finance, Now)).Error);
        bill.Pay(Today, 60m, OperationsBillPaymentMethod.Cheque, "512", Finance, Today, Now);
        Assert.Equal((OperationsBillStatus.Paid, 0m, 2), (bill.Status, bill.Owed, bill.Payments.Count));

        OperationsBill mistake = Bill(25m);
        mistake.Void("Entered twice", Finance, Now);
        Assert.Equal((OperationsBillStatus.Voided, false), (mistake.Status, mistake.Counts));
    }
}
