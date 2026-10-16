using System.Globalization;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformNumberSequence.cs
//
// PURPOSE:
// The next number for invoices, credit notes or receipts in one year (D-B2).
//
// RESPONSIBILITIES:
// - Hand out numbers one by one: INV-2026-000001, INV-2026-000002...
//
// IMPORTANT NOTES:
// - Gap-free: the number is taken in the same commit as the document; a race fails on the row version and is retried
// - One row per kind and year
// =========================================================
public sealed class PlatformNumberSequence : AuditableEntity
{
    private PlatformNumberSequence()
    {
    }

    private PlatformNumberSequence(PlatformBillingDocumentKind kind, int year, DateTimeOffset utcNow)
        : base(utcNow)
    {
        Kind = kind;
        Year = year;
        NextValue = 1;
    }

    public PlatformBillingDocumentKind Kind { get; private set; }

    public int Year { get; private set; }

    public int NextValue { get; private set; }

    public static PlatformNumberSequence Start(PlatformBillingDocumentKind kind, int year, DateTimeOffset utcNow)
    {
        return new PlatformNumberSequence(kind, year, utcNow);
    }

    public string Take(string prefix, DateTimeOffset utcNow)
    {
        if (NextValue > PlatformBillingLimits.MaxNumber)
        {
            throw new DomainException(PlatformBillingErrors.SequenceExhausted);
        }

        string number = string.Create(CultureInfo.InvariantCulture, $"{prefix}-{Year}-{NextValue:D6}");
        NextValue++;
        MarkAsModified(utcNow);

        return number;
    }
}
