using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformOffer.cs
//
// PURPOSE:
// Agreed terms for one valued prospect, reached through a private registration link (D-S21, D-S24).
//
// RESPONSIBILITIES:
// - Hold the plan, term, licences, discount, setup fee, one-time charges, trial, expiry and uses
// - Go from Draft to Issued directly or through sales manager approval
// - Check and record a use by the prospect
//
// IMPORTANT NOTES:
// - Aggregate root. Only the SHA-256 hash of the link token is stored; the token is shown once when issued
// - A new link can be issued; the old one then stops working
// - Nobody approves their own offer
// =========================================================
public sealed class PlatformOffer : AuditableEntity
{
    public const int CompanyMaxLength = 200;
    public const int MaxUses = 100;

    private readonly List<PlatformOfferCharge> _charges = new();

    private PlatformOffer()
    {
        CompanyName = string.Empty;
        Reason = string.Empty;
    }

    private PlatformOffer(Guid preparedByUserId, DateTimeOffset utcNow)
        : base(utcNow)
    {
        CompanyName = string.Empty;
        Reason = string.Empty;
        PreparedByUserId = preparedByUserId;
        Status = PlatformOfferStatus.Draft;
    }

    public string? TokenHash { get; private set; }

    public string? BoundEmail { get; private set; }

    public string CompanyName { get; private set; }

    public string Reason { get; private set; }

    public Guid PlatformPlanId { get; private set; }

    public PlatformSubscriptionTerm Term { get; private set; }

    public int Licences { get; private set; }

    public PlatformDiscountTerms? Discount { get; private set; }

    public decimal? SetupFeeOverride { get; private set; }

    public bool StartTrial { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public int UsesAllowed { get; private set; }

    public int Uses { get; private set; }

    public PlatformOfferStatus Status { get; private set; }

    public Guid PreparedByUserId { get; private set; }

    public DateTimeOffset? SubmittedUtc { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTimeOffset? DecidedUtc { get; private set; }

    public string? DecisionReason { get; private set; }

    public DateTimeOffset? IssuedUtc { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public DateTimeOffset? RevokedUtc { get; private set; }

    public IReadOnlyList<PlatformOfferCharge> Charges => _charges;

    public bool IsEditable => Status is PlatformOfferStatus.Draft or PlatformOfferStatus.Rejected;

    public bool IsExpiredAt(DateTimeOffset utcNow) => ExpiresUtc <= utcNow;

    // =====================================================
    // METHOD: PREPARE AND EDIT
    // =====================================================
    public static PlatformOffer Prepare(
        string? companyName,
        string? boundEmail,
        string? reason,
        Guid platformPlanId,
        PlatformSubscriptionTerm term,
        int licences,
        PlatformDiscountTerms? discount,
        decimal? setupFeeOverride,
        IReadOnlyList<(string Description, decimal Amount)> charges,
        bool startTrial,
        DateTimeOffset expiresUtc,
        int usesAllowed,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        Guid actor = actorUserId == Guid.Empty ? throw new DomainException(PlatformSalesErrors.ActorRequired) : actorUserId;
        PlatformOffer offer = new(actor, utcNow);
        offer.Define(companyName, boundEmail, reason, platformPlanId, term, licences, discount, setupFeeOverride, charges, startTrial, expiresUtc, usesAllowed, utcNow);

        return offer;
    }

    public void Edit(
        string? companyName,
        string? boundEmail,
        string? reason,
        Guid platformPlanId,
        PlatformSubscriptionTerm term,
        int licences,
        PlatformDiscountTerms? discount,
        decimal? setupFeeOverride,
        IReadOnlyList<(string Description, decimal Amount)> charges,
        bool startTrial,
        DateTimeOffset expiresUtc,
        int usesAllowed,
        DateTimeOffset utcNow)
    {
        if (!IsEditable)
        {
            throw new DomainException(PlatformSalesErrors.OfferNotEditable);
        }

        Define(companyName, boundEmail, reason, platformPlanId, term, licences, discount, setupFeeOverride, charges, startTrial, expiresUtc, usesAllowed, utcNow);
        Status = PlatformOfferStatus.Draft;
    }

    // =====================================================
    // METHOD: ISSUE AND APPROVAL (D-S24)
    // =====================================================
    public void Submit(bool needsApproval, string? tokenHash, DateTimeOffset utcNow)
    {
        if (!IsEditable)
        {
            throw new DomainException(PlatformSalesErrors.OfferNotEditable);
        }

        if (IsExpiredAt(utcNow))
        {
            throw new DomainException(PlatformSalesErrors.OfferExpiryInvalid);
        }

        SubmittedUtc = utcNow;
        DecidedByUserId = null;
        DecidedUtc = null;
        DecisionReason = null;

        if (needsApproval)
        {
            Status = PlatformOfferStatus.PendingApproval;
            MarkAsModified(utcNow);
            return;
        }

        Issue(tokenHash, utcNow);
    }

    public void Approve(Guid actorUserId, string? reason, string tokenHash, DateTimeOffset utcNow)
    {
        Decide(actorUserId, reason, utcNow);
        Issue(tokenHash, utcNow);
    }

    public void Reject(Guid actorUserId, string? reason, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(PlatformSalesErrors.ReasonRequired);
        }

        Decide(actorUserId, reason, utcNow);
        Status = PlatformOfferStatus.Rejected;
        MarkAsModified(utcNow);
    }

    public void ReplaceLink(string tokenHash, DateTimeOffset utcNow)
    {
        if (Status != PlatformOfferStatus.Issued || IsExpiredAt(utcNow))
        {
            throw new DomainException(PlatformSalesErrors.OfferNotIssued);
        }

        Issue(tokenHash, utcNow);
    }

    public void Revoke(Guid actorUserId, DateTimeOffset utcNow)
    {
        if (Status is PlatformOfferStatus.Used or PlatformOfferStatus.Revoked)
        {
            throw new DomainException(PlatformSalesErrors.OfferNotRevocable);
        }

        Status = PlatformOfferStatus.Revoked;
        RevokedByUserId = actorUserId;
        RevokedUtc = utcNow;
        TokenHash = null;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: USE (D-S21)
    // =====================================================
    public Vixly.Shared.Kernel.Errors.Error? Refusal(string? email, DateTimeOffset utcNow)
    {
        if (Status != PlatformOfferStatus.Issued || IsExpiredAt(utcNow))
        {
            return PlatformSalesErrors.OfferUnavailable;
        }

        return BoundEmail is not null && !string.Equals(BoundEmail, email?.Trim(), StringComparison.OrdinalIgnoreCase)
            ? PlatformSalesErrors.OfferEmailMismatch
            : null;
    }

    public void Use(string? email, DateTimeOffset utcNow)
    {
        if (Refusal(email, utcNow) is { } refused)
        {
            throw new DomainException(refused);
        }

        Uses++;

        if (Uses >= UsesAllowed)
        {
            Status = PlatformOfferStatus.Used;
            TokenHash = null;
        }

        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: HELPERS
    // =====================================================
    private void Define(
        string? companyName,
        string? boundEmail,
        string? reason,
        Guid platformPlanId,
        PlatformSubscriptionTerm term,
        int licences,
        PlatformDiscountTerms? discount,
        decimal? setupFeeOverride,
        IReadOnlyList<(string Description, decimal Amount)> charges,
        bool startTrial,
        DateTimeOffset expiresUtc,
        int usesAllowed,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(charges);
        string company = companyName?.Trim() ?? string.Empty;

        if (company.Length == 0 || company.Length > CompanyMaxLength)
        {
            throw new DomainException(PlatformSalesErrors.OfferCompanyRequired);
        }

        string why = reason?.Trim() ?? string.Empty;

        if (why.Length == 0)
        {
            throw new DomainException(PlatformSalesErrors.ReasonRequired);
        }

        if (why.Length > PlatformSubscriptionLimits.ReasonMaxLength)
        {
            throw new DomainException(PlatformSalesErrors.ReasonTooLong);
        }

        string? email = null;

        if (!string.IsNullOrWhiteSpace(boundEmail))
        {
            try
            {
                email = EmailAddress.Create(boundEmail).Value;
            }
            catch (DomainException)
            {
                throw new DomainException(PlatformSalesErrors.OfferEmailInvalid);
            }
        }

        if (platformPlanId == Guid.Empty || !Enum.IsDefined(term) || licences < 1 || licences > PlatformSubscriptionLimits.MaxLicences)
        {
            throw new DomainException(PlatformSalesErrors.OfferTermsInvalid);
        }

        if (setupFeeOverride is decimal fee && (fee < 0m || fee > PlatformSubscriptionLimits.MaxPrice || decimal.Round(fee, 2) != fee))
        {
            throw new DomainException(PlatformSalesErrors.OfferTermsInvalid);
        }

        foreach ((string description, decimal amount) in charges)
        {
            if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > PlatformDiscountTerms.ChargeDescriptionMaxLength
                || amount <= 0m || amount > PlatformSubscriptionLimits.MaxPrice || decimal.Round(amount, 2) != amount)
            {
                throw new DomainException(PlatformSalesErrors.OfferTermsInvalid);
            }
        }

        if (expiresUtc <= utcNow || expiresUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException(PlatformSalesErrors.OfferExpiryInvalid);
        }

        if (usesAllowed is < 1 or > MaxUses)
        {
            throw new DomainException(PlatformSalesErrors.OfferUsesInvalid);
        }

        CompanyName = company;
        BoundEmail = email;
        Reason = why;
        PlatformPlanId = platformPlanId;
        Term = term;
        Licences = licences;
        Discount = discount;
        SetupFeeOverride = setupFeeOverride;
        StartTrial = startTrial;
        ExpiresUtc = expiresUtc;
        UsesAllowed = usesAllowed;
        _charges.Clear();
        _charges.AddRange(charges.Select(item => new PlatformOfferCharge(item.Description.Trim(), item.Amount)));
        MarkAsModified(utcNow);
    }

    private void Decide(Guid actorUserId, string? reason, DateTimeOffset utcNow)
    {
        if (Status != PlatformOfferStatus.PendingApproval)
        {
            throw new DomainException(PlatformSalesErrors.OfferNotPending);
        }

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformSalesErrors.ActorRequired);
        }

        if (actorUserId == PreparedByUserId)
        {
            throw new DomainException(PlatformSalesErrors.OwnSubmission);
        }

        string? why = reason?.Trim();

        if (why is { Length: > PlatformSubscriptionLimits.ReasonMaxLength })
        {
            throw new DomainException(PlatformSalesErrors.ReasonTooLong);
        }

        DecidedByUserId = actorUserId;
        DecidedUtc = utcNow;
        DecisionReason = string.IsNullOrEmpty(why) ? null : why;
    }

    private void Issue(string? tokenHash, DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException(PlatformSalesErrors.OfferNotIssued);
        }

        TokenHash = tokenHash;
        Status = PlatformOfferStatus.Issued;
        IssuedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
