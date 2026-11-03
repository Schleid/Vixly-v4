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
//
// IMPORTANT NOTES:
// - Never without the customer's agreement: the staff member confirms it, and it is recorded
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

    public Guid ConsentRecordedByUserId { get; private set; }

    public DateTimeOffset ConsentRecordedUtc { get; private set; }

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
        Shown = shown;
        MarkAsModified(utcNow);
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
