using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Screens;

// =========================================================
// FILE: OperationsCustomerScreen.cs
//
// PURPOSE:
// A tablet or touch screen at a warehouse's counter that the customer uses (Slice 5F step 3, P-2, P-3, P-9).
//
// RESPONSIBILITIES:
// - Be paired with a short code typed on the screen once (valid 15 minutes); the screen then holds a long secret in its own cookie (both kept as hashes)
// - Show what a clerk sends: an invoice to pay, or a shipment to sign; follow the customer's steps; clear for the next customer
// - Keep the customer's own acceptance of the Zelle terms (when, on which screen, the clerk present)
//
// IMPORTANT NOTES:
// - A screen holds no staff rights: it shows only what is sent to it now
// - One thing at a time; a new send replaces it
// - Removed screens stop working at once (their secret no longer matches anything)
// =========================================================
public sealed class OperationsCustomerScreen : OperationsEntity
{
    public const int NameMaxLength = 60;

    private OperationsCustomerScreen()
    {
        Name = string.Empty;
        SecretHash = string.Empty;
    }

    private OperationsCustomerScreen(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Name = string.Empty;
        SecretHash = string.Empty;
    }

    public Guid WarehouseId { get; private set; }

    public string Name { get; private set; }

    // Empty until the screen is paired with its code.
    public string SecretHash { get; private set; }

    // The code typed on the screen to pair it; one use, short-lived.
    public string? PairingCodeHash { get; private set; }

    public DateTimeOffset? PairingExpiresUtc { get; private set; }

    public bool Paired => SecretHash.Length > 0;

    public Guid PairedByUserId { get; private set; }

    public DateTimeOffset? LastSeenUtc { get; private set; }

    public OperationsScreenStep Step { get; private set; }

    // What is on the screen now, and who sent it.
    public Guid? InvoiceId { get; private set; }

    public Guid? ShipmentId { get; private set; }

    public Guid? SentByUserId { get; private set; }

    public DateTimeOffset? SentUtc { get; private set; }

    // The customer accepted the Zelle terms here for this invoice (P-9): kept, not ticked by the clerk.
    public DateTimeOffset? ZelleAcceptedUtc { get; private set; }

    public Guid? CardChargeId { get; private set; }

    public static readonly TimeSpan PairingLifetime = TimeSpan.FromMinutes(15);

    public static OperationsCustomerScreen Add(Guid tenantId, Guid warehouseId, string? name, string pairingCodeHash, Guid addedBy, DateTimeOffset utcNow)
    {
        string label = (name ?? string.Empty).Trim();

        if (warehouseId == Guid.Empty || label.Length is 0 or > NameMaxLength || label.Any(char.IsControl))
        {
            throw new DomainException(OperationsScreenErrors.NameInvalid);
        }

        OperationsCustomerScreen screen = new(tenantId, utcNow) { WarehouseId = warehouseId, Name = label };
        screen.NewCode(pairingCodeHash, addedBy, utcNow);
        return screen;
    }

    // A new code (a new tablet, or the old one lost): the screen paired before stops working at once.
    public void NewCode(string pairingCodeHash, Guid by, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pairingCodeHash);
        (PairingCodeHash, PairingExpiresUtc, SecretHash, PairedByUserId) = (pairingCodeHash, utcNow + PairingLifetime, string.Empty, by);
        Reset(null, utcNow);
    }

    public bool CodeValid(DateTimeOffset utcNow) => PairingCodeHash is not null && PairingExpiresUtc > utcNow;

    // The code typed on the screen: it now holds its own secret, and the code is spent.
    public void Pair(string secretHash, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretHash);

        if (!CodeValid(utcNow))
        {
            throw new DomainException(OperationsScreenErrors.NotPaired);
        }

        (SecretHash, PairingCodeHash, PairingExpiresUtc, LastSeenUtc) = (secretHash, null, null, utcNow);
        MarkAsModified(utcNow);
    }

    public void Seen(DateTimeOffset utcNow)
    {
        // At most once a minute: a screen asks every few seconds.
        if (LastSeenUtc is not { } seen || utcNow - seen > TimeSpan.FromMinutes(1))
        {
            LastSeenUtc = utcNow;
        }
    }

    public void ShowInvoice(Guid invoiceId, Guid sentBy, DateTimeOffset utcNow)
    {
        Reset(sentBy, utcNow);
        (InvoiceId, Step) = (invoiceId == Guid.Empty ? throw new DomainException(OperationsScreenErrors.StepInvalid) : invoiceId, OperationsScreenStep.Summary);
    }

    public void ShowSignature(Guid shipmentId, Guid sentBy, DateTimeOffset utcNow)
    {
        Reset(sentBy, utcNow);
        (ShipmentId, Step) = (shipmentId == Guid.Empty ? throw new DomainException(OperationsScreenErrors.StepInvalid) : shipmentId, OperationsScreenStep.Signature);
    }

    // The customer signed the declaration shown.
    public void Signed(DateTimeOffset utcNow)
    {
        if (Step != OperationsScreenStep.Signature || ShipmentId is null)
        {
            throw new DomainException(OperationsScreenErrors.StepInvalid);
        }

        Step = OperationsScreenStep.Done;
        MarkAsModified(utcNow);
    }

    // The customer's choices, only from the step that offers them.
    public void ChooseZelle(DateTimeOffset utcNow) => Move(OperationsScreenStep.Summary, ZelleAcceptedUtc is null ? OperationsScreenStep.ZelleTerms : OperationsScreenStep.Zelle, utcNow);

    public void AcceptZelle(DateTimeOffset utcNow)
    {
        Move(OperationsScreenStep.ZelleTerms, OperationsScreenStep.Zelle, utcNow);
        ZelleAcceptedUtc = utcNow;
    }

    public void ChooseCard(Guid chargeId, DateTimeOffset utcNow)
    {
        if (Step is not (OperationsScreenStep.Summary or OperationsScreenStep.Card) || InvoiceId is null)
        {
            throw new DomainException(OperationsScreenErrors.StepInvalid);
        }

        (Step, CardChargeId) = (OperationsScreenStep.Card, chargeId);
        MarkAsModified(utcNow);
    }

    public void Back(DateTimeOffset utcNow)
    {
        if (InvoiceId is not null && Step is OperationsScreenStep.ZelleTerms or OperationsScreenStep.Zelle or OperationsScreenStep.Card)
        {
            Step = OperationsScreenStep.Summary;
            MarkAsModified(utcNow);
        }
    }

    public void Finish(DateTimeOffset utcNow)
    {
        if (Step != OperationsScreenStep.Idle)
        {
            Step = OperationsScreenStep.Done;
            MarkAsModified(utcNow);
        }
    }

    // Clear · next customer.
    public void Clear(DateTimeOffset utcNow) => Reset(null, utcNow);

    // How long a screen keeps what was sent, and the thank-you page, before it clears by itself (review: never left for the next customer).
    public static readonly TimeSpan SentLifetime = TimeSpan.FromMinutes(20);

    public static readonly TimeSpan DoneLifetime = TimeSpan.FromMinutes(1);

    // Paid meanwhile (at the cashier, by card, by Zelle): thank you; then, and after too long, clear. Returns whether it changed.
    public bool Tidy(bool invoiceSettled, DateTimeOffset utcNow)
    {
        OperationsScreenStep before = Step;

        if (Step == OperationsScreenStep.Done && utcNow - (LastModifiedUtc ?? CreatedUtc) > DoneLifetime)
        {
            Reset(null, utcNow);
        }
        else if (Step != OperationsScreenStep.Idle && SentUtc is { } sent && utcNow - sent > SentLifetime)
        {
            Reset(null, utcNow);
        }
        else if (invoiceSettled && InvoiceId is not null && Step is OperationsScreenStep.Summary or OperationsScreenStep.ZelleTerms or OperationsScreenStep.Card)
        {
            Step = OperationsScreenStep.Done;
            MarkAsModified(utcNow);
        }

        return Step != before;
    }

    private void Move(OperationsScreenStep from, OperationsScreenStep to, DateTimeOffset utcNow)
    {
        if (Step != from || InvoiceId is null)
        {
            throw new DomainException(OperationsScreenErrors.StepInvalid);
        }

        Step = to;
        MarkAsModified(utcNow);
    }

    private void Reset(Guid? sentBy, DateTimeOffset utcNow)
    {
        (Step, InvoiceId, ShipmentId, ZelleAcceptedUtc, CardChargeId) = (OperationsScreenStep.Idle, null, null, null, null);
        (SentByUserId, SentUtc) = (sentBy, sentBy is null ? null : utcNow);
        MarkAsModified(utcNow);
    }
}
