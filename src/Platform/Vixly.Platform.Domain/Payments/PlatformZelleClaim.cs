using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformZelleClaim.cs
//
// PURPOSE:
// A customer's notice that an invoice was paid with Zelle, waiting
// for Vixly staff to check the money arrived (Slice 5 update).
//
// RESPONSIBILITIES:
// - Check the notice: US dollars, an amount up to the balance, a recent send date, sender and Zelle confirmation code
// - Confirm (staff found the money; a payment is recorded) or reject with a reason the customer sees
//
// IMPORTANT NOTES:
// - A claim never pays the invoice by itself; only a staff confirmation records a payment
// - One waiting claim per invoice, enforced by a filtered unique index too
// - Row version: two staff members never decide one claim twice
// =========================================================
public sealed class PlatformZelleClaim : AuditableEntity
{
    private PlatformZelleClaim()
    {
        InvoiceNumber = string.Empty;
        CurrencyCode = PlatformZelleLimits.Currency;
        SenderName = string.Empty;
        Reference = string.Empty;
    }

    private PlatformZelleClaim(Guid tenantId, Guid invoiceId, string invoiceNumber, decimal amount, DateTime sentOn, string senderName, string reference, PlatformZelleClaimSource source, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformTenantId = tenantId;
        PlatformInvoiceId = invoiceId;
        InvoiceNumber = invoiceNumber;
        CurrencyCode = PlatformZelleLimits.Currency;
        Amount = amount;
        SentOn = sentOn;
        SenderName = senderName;
        Reference = reference;
        Source = source;
        Status = PlatformZelleClaimStatus.Waiting;
    }

    public Guid PlatformTenantId { get; private set; }

    public Guid PlatformInvoiceId { get; private set; }

    public string InvoiceNumber { get; private set; }

    public string CurrencyCode { get; private set; }

    public decimal Amount { get; private set; }

    // The day the customer says the money was sent (date only).
    public DateTime SentOn { get; private set; }

    public string SenderName { get; private set; }

    // The Zelle confirmation code from the customer's bank.
    public string Reference { get; private set; }

    public PlatformZelleClaimSource Source { get; private set; }

    public PlatformZelleClaimStatus Status { get; private set; }

    public decimal? ReceivedAmount { get; private set; }

    public DateTimeOffset? ReceivedUtc { get; private set; }

    public Guid? PlatformPaymentId { get; private set; }

    public string? RejectReason { get; private set; }

    public string? StaffNote { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTimeOffset? DecidedUtc { get; private set; }

    public bool IsWaiting => Status == PlatformZelleClaimStatus.Waiting;

    public static PlatformZelleClaim Submit(
        Guid tenantId,
        Guid invoiceId,
        string invoiceNumber,
        string currencyCode,
        decimal balance,
        decimal amount,
        DateTime? sentOn,
        string? senderName,
        string? reference,
        PlatformZelleClaimSource source,
        DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);

        if (!string.Equals(currencyCode, PlatformZelleLimits.Currency, StringComparison.Ordinal) || balance <= 0m || !Enum.IsDefined(source))
        {
            throw new DomainException(PlatformZelleErrors.NotAvailable);
        }

        if (amount <= 0m || amount > balance || decimal.Round(amount, 2) != amount)
        {
            throw new DomainException(PlatformZelleErrors.AmountInvalid);
        }

        // The customer's own day may be one ahead of UTC.
        DateTime today = utcNow.UtcDateTime.Date;

        if (sentOn is not { } day || day.Date > today.AddDays(1) || day.Date < today.AddDays(-PlatformZelleLimits.MaxDaysBack))
        {
            throw new DomainException(PlatformZelleErrors.DateInvalid);
        }

        string sender = senderName?.Trim() ?? string.Empty;

        if (sender.Length == 0 || sender.Length > PlatformZelleLimits.SenderMaxLength || sender.Any(char.IsControl))
        {
            throw new DomainException(PlatformZelleErrors.SenderInvalid);
        }

        string code = reference?.Trim() ?? string.Empty;

        if (code.Length == 0 || code.Length > PlatformZelleLimits.ReferenceMaxLength || code.Any(char.IsControl))
        {
            throw new DomainException(PlatformZelleErrors.ReferenceInvalid);
        }

        return new PlatformZelleClaim(tenantId, invoiceId, invoiceNumber, amount, DateTime.SpecifyKind(day.Date, DateTimeKind.Unspecified), sender, code, source, utcNow);
    }

    // Staff found the money: the received amount may differ from the claim (never above the balance; the payment rules check that).
    public void Confirm(decimal receivedAmount, DateTimeOffset receivedUtc, Guid paymentId, string? note, Guid staffUserId, DateTimeOffset utcNow)
    {
        Decide(staffUserId);

        if (receivedAmount <= 0m)
        {
            throw new DomainException(PlatformZelleErrors.AmountInvalid);
        }

        Status = PlatformZelleClaimStatus.Confirmed;
        ReceivedAmount = receivedAmount;
        ReceivedUtc = receivedUtc;
        PlatformPaymentId = paymentId;
        StaffNote = Note(note);
        DecidedByUserId = staffUserId;
        DecidedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // The reason goes to the customer; the note stays internal.
    public void Reject(string? reason, string? note, Guid staffUserId, DateTimeOffset utcNow)
    {
        Decide(staffUserId);
        string text = reason?.Trim() ?? string.Empty;

        if (text.Length == 0 || text.Length > PlatformZelleLimits.ReasonMaxLength)
        {
            throw new DomainException(PlatformZelleErrors.ReasonRequired);
        }

        Status = PlatformZelleClaimStatus.Rejected;
        RejectReason = text;
        StaffNote = Note(note);
        DecidedByUserId = staffUserId;
        DecidedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    private void Decide(Guid staffUserId)
    {
        if (staffUserId == Guid.Empty)
        {
            throw new DomainException(PlatformZelleErrors.ActorRequired);
        }

        if (!IsWaiting)
        {
            throw new DomainException(PlatformZelleErrors.ClaimDecided);
        }
    }

    private static string? Note(string? note)
    {
        string? text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        return text is { Length: > PlatformZelleLimits.NoteMaxLength } ? text[..PlatformZelleLimits.NoteMaxLength] : text;
    }
}
