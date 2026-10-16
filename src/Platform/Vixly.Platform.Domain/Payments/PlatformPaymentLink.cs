using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformPaymentLink.cs
//
// PURPOSE:
// A link that lets a tenant pay an invoice or save a card on the provider's page without signing in.
//
// RESPONSIBILITIES:
// - Hold the hash of a random token, what it opens and until when
// - Mark a save-card link used once the card is saved
//
// IMPORTANT NOTES:
// - Only the SHA-256 of the token is stored; the token itself appears only in the email
// - A pay link stays usable until it expires or the invoice is settled
// =========================================================
public sealed class PlatformPaymentLink : AuditableEntity
{
    private PlatformPaymentLink()
    {
        TokenHash = string.Empty;
    }

    private PlatformPaymentLink(PlatformPaymentLinkKind kind, Guid tenantId, Guid? invoiceId, string tokenHash, DateTimeOffset expiresUtc, Guid? createdByUserId, DateTimeOffset utcNow)
        : base(utcNow)
    {
        Kind = kind;
        PlatformTenantId = tenantId;
        PlatformInvoiceId = invoiceId;
        TokenHash = tokenHash;
        ExpiresUtc = expiresUtc;
        CreatedByUserId = createdByUserId;
    }

    public PlatformPaymentLinkKind Kind { get; private set; }

    public Guid PlatformTenantId { get; private set; }

    public Guid? PlatformInvoiceId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public DateTimeOffset? UsedUtc { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public static PlatformPaymentLink Create(PlatformPaymentLinkKind kind, Guid tenantId, Guid? invoiceId, string? tokenHash, Guid? createdByUserId, DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(kind) || tenantId == Guid.Empty || (kind == PlatformPaymentLinkKind.PayInvoice) != (invoiceId is not null)
            || tokenHash is null || tokenHash.Length != PlatformPaymentsLimits.TokenHashLength)
        {
            throw new DomainException(PlatformPaymentsErrors.LinkInvalid);
        }

        TimeSpan lifetime = kind == PlatformPaymentLinkKind.PayInvoice ? PlatformPaymentsLimits.PayLinkLifetime : PlatformPaymentsLimits.SaveCardLinkLifetime;

        return new PlatformPaymentLink(kind, tenantId, invoiceId, tokenHash, utcNow.Add(lifetime), createdByUserId, utcNow);
    }

    public bool IsUsableAt(DateTimeOffset utcNow) => UsedUtc is null && utcNow < ExpiresUtc;

    public void MarkUsed(DateTimeOffset utcNow)
    {
        if (UsedUtc is null)
        {
            UsedUtc = utcNow;
            MarkAsModified(utcNow);
        }
    }
}
