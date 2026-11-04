using System.Text.RegularExpressions;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsSiteTestimonial.cs
//
// PURPOSE:
// A customer's own words about the company, shown on its website (owner, 2026-10-30).
//
// RESPONSIBILITIES:
// - Hold the name as the customer agreed to show it, the place, the quote, an optional rating and where it was given
// - Record who confirmed the customer's agreement to be quoted, and when
// - Show or hide it on the website, in the company's order
// - A customer's own review from the portal (owner, 2026-11-03): one per customer, new until the company publishes or hides it
//
// IMPORTANT NOTES:
// - Never without the customer's agreement: the staff member confirms it, or the customer gives it in the portal; both are recorded
// - The company never edits a customer's words; a change by the customer makes the review new again
// - Plain text only; every page encodes output. Nothing is ever invented: no testimonial, no section
// =========================================================
public sealed class OperationsSiteTestimonial : OperationsEntity
{
    public const int NameMaxLength = 80;

    public const int PlaceMaxLength = 80;

    public const int QuoteMaxLength = 400;

    public const int SourceMaxLength = 40;

    public const int MaxPerCompany = 30;

    public const int ShownOnSite = 6;

    private static readonly Regex Markup = new("[<>]", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private OperationsSiteTestimonial()
    {
        Name = string.Empty;
        Quote = string.Empty;
    }

    private OperationsSiteTestimonial(Guid tenantId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        Name = string.Empty;
        Quote = string.Empty;
    }

    public string Name { get; private set; }

    public string? Place { get; private set; }

    public string Quote { get; private set; }

    public int? Rating { get; private set; }

    // Where the customer said it, such as Google, Facebook or at the counter.
    public string? Source { get; private set; }

    // Staff-entered: who confirmed the customer's agreement. A customer review has none (the customer agreed in the portal).
    public Guid? ConsentRecordedByUserId { get; private set; }

    public DateTimeOffset? ConsentRecordedUtc { get; private set; }

    // A customer review (owner, 2026-11-03): its author, when they agreed to be quoted (none: feedback only), and when they last sent it.
    public Guid? CustomerId { get; private set; }

    public DateTimeOffset? CustomerAgreedUtc { get; private set; }

    public DateTimeOffset? SubmittedUtc { get; private set; }

    public OperationsReviewNameStyle? NameStyle { get; private set; }

    // When the company last published or hid it; none means new. Why it was hidden, when it was.
    public DateTimeOffset? ReviewedUtc { get; private set; }

    public OperationsReviewHideReason? HiddenReason { get; private set; }

    public bool FromCustomer => CustomerId is not null;

    public bool IsNew => FromCustomer && ReviewedUtc is null;

    public bool MayBePublished => !FromCustomer || CustomerAgreedUtc is not null;

    public bool Shown { get; private set; }

    public int SortOrder { get; private set; }

    public static OperationsSiteTestimonial Record(Guid tenantId, string? name, string? place, string? quote, int? rating, string? source, bool customerAgreed,
        Guid recordedByUserId, int sortOrder, DateTimeOffset utcNow)
    {
        if (!customerAgreed || recordedByUserId == Guid.Empty)
        {
            throw new DomainException(OperationsBrandingErrors.TestimonialConsentRequired);
        }

        if (rating is { } stars && (stars < 1 || stars > 5))
        {
            throw new DomainException(OperationsBrandingErrors.TestimonialRatingInvalid);
        }

        return new OperationsSiteTestimonial(tenantId, utcNow)
        {
            Name = Required(name, NameMaxLength, OperationsBrandingErrors.TestimonialNameRequired),
            Place = Optional(place, PlaceMaxLength),
            Quote = Required(quote, QuoteMaxLength, OperationsBrandingErrors.TestimonialQuoteRequired),
            Rating = rating,
            Source = Optional(source, SourceMaxLength),
            ConsentRecordedByUserId = recordedByUserId,
            ConsentRecordedUtc = utcNow,
            Shown = true,
            SortOrder = Math.Clamp(sortOrder, 0, 999)
        };
    }

    public void ChangeShown(bool shown, DateTimeOffset utcNow)
    {
        if (shown && !MayBePublished)
        {
            throw new DomainException(OperationsBrandingErrors.ReviewNotAgreed);
        }

        Shown = shown;

        if (FromCustomer)
        {
            ReviewedUtc = utcNow;
            HiddenReason = null;
        }

        MarkAsModified(utcNow);
    }

    // The customer's review in the portal: their words, a rating, how their name appears, and whether the company may quote it.
    public static OperationsSiteTestimonial Submit(Guid tenantId, Guid customerId, string? firstName, string? lastName, OperationsReviewNameStyle nameStyle, string? place,
        string? quote, int? rating, bool agreed, DateTimeOffset utcNow)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException(OperationsBrandingErrors.TestimonialNotFound);
        }

        OperationsSiteTestimonial review = new(tenantId, utcNow) { CustomerId = customerId, SortOrder = 999 };
        review.Write(firstName, lastName, nameStyle, place, quote, rating, agreed, utcNow);
        return review;
    }

    // The customer changes it: new again for the company, off the website until published again.
    public void Rewrite(string? firstName, string? lastName, OperationsReviewNameStyle nameStyle, string? place, string? quote, int? rating, bool agreed, DateTimeOffset utcNow)
    {
        if (!FromCustomer)
        {
            throw new DomainException(OperationsBrandingErrors.TestimonialNotFound);
        }

        Write(firstName, lastName, nameStyle, place, quote, rating, agreed, utcNow);
        MarkAsModified(utcNow);
    }

    // The company hides a customer review, saying why (kept for the company, never shown to the customer).
    public void Hide(OperationsReviewHideReason? reason, DateTimeOffset utcNow)
    {
        if (reason is { } given && !Enum.IsDefined(given))
        {
            throw new DomainException(OperationsBrandingErrors.ReviewHideReasonInvalid);
        }

        Shown = false;

        if (FromCustomer)
        {
            ReviewedUtc = utcNow;
            HiddenReason = reason;
        }

        MarkAsModified(utcNow);
    }

    public void MoveTo(int sortOrder, DateTimeOffset utcNow)
    {
        SortOrder = Math.Clamp(sortOrder, 0, 999);
        MarkAsModified(utcNow);
    }

    // "Marie P." or "Marie": never the full name.
    public static string ShownName(string? firstName, string? lastName, OperationsReviewNameStyle style)
    {
        string first = (firstName ?? string.Empty).Trim();
        string last = (lastName ?? string.Empty).Trim();

        if (first.Length == 0)
        {
            throw new DomainException(OperationsBrandingErrors.TestimonialNameRequired);
        }

        return style == OperationsReviewNameStyle.FirstAndInitial && last.Length > 0
            ? first + " " + char.ToUpperInvariant(last[0]) + "."
            : first;
    }

    private void Write(string? firstName, string? lastName, OperationsReviewNameStyle nameStyle, string? place, string? quote, int? rating, bool agreed, DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(nameStyle))
        {
            throw new DomainException(OperationsBrandingErrors.TestimonialNameRequired);
        }

        if (rating is not { } stars || stars < 1 || stars > 5)
        {
            throw new DomainException(OperationsBrandingErrors.TestimonialRatingInvalid);
        }

        string name = ShownName(firstName, lastName, nameStyle);
        Name = name.Length > NameMaxLength ? name[..NameMaxLength] : name;
        NameStyle = nameStyle;
        Place = Optional(place, PlaceMaxLength);
        Quote = Required(quote, QuoteMaxLength, OperationsBrandingErrors.TestimonialQuoteRequired);
        Rating = stars;
        CustomerAgreedUtc = agreed ? CustomerAgreedUtc ?? utcNow : null;
        SubmittedUtc = utcNow;
        Shown = false;
        ReviewedUtc = null;
        HiddenReason = null;
    }

    private static string Required(string? value, int max, Vixly.Shared.Kernel.Errors.Error missing) =>
        Optional(value, max) ?? throw new DomainException(missing);

    private static string? Optional(string? value, int max)
    {
        string? trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        if (trimmed is null)
        {
            return null;
        }

        if (trimmed.Length > max)
        {
            throw new DomainException(OperationsBrandingErrors.TestimonialTooLong);
        }

        return Markup.IsMatch(trimmed) ? throw new DomainException(OperationsBrandingErrors.TextNotPlain) : trimmed;
    }
}
