using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Shared.Kernel.Localization;

// =========================================================
// FILE: LanguageCode.cs
//
// PURPOSE:
// A supported user-interface language: English, French or
// Spanish.
//
// RESPONSIBILITIES:
// - Be the only representation of a language in code
// - Normalize input such as "FR", "fr-CA" or "es_MX" to the
//   neutral language code
// - Reject unsupported languages
//
// IMPORTANT NOTES:
// - English is the primary language, the source of all text
//   and the fallback (ADR-0005)
// - Adding a language needs an ADR and complete resources for
//   every project that owns text
// - Neutral codes only; regional wording variants are not
//   supported
// =========================================================
public sealed record LanguageCode
{
    public static LanguageCode En { get; } = new("en");

    public static LanguageCode Fr { get; } = new("fr");

    public static LanguageCode Es { get; } = new("es");

    public static LanguageCode Default => En;

    public static IReadOnlyList<LanguageCode> All { get; } = new[] { En, Fr, Es };

    private LanguageCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    // =====================================================
    // METHOD: CREATE
    // =====================================================
    public static LanguageCode Create(string? value)
    {
        if (!TryCreate(value, out LanguageCode? language))
        {
            throw new DomainException(KernelErrors.LanguageUnsupported);
        }

        return language;
    }

    // =====================================================
    // METHOD: TRY CREATE
    // =====================================================
    public static bool TryCreate(
        string? value,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out LanguageCode? language)
    {
        language = null;

        string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        int separator = normalized.IndexOfAny(new[] { '-', '_' });

        if (separator > 0)
        {
            normalized = normalized[..separator];
        }

        language = All.FirstOrDefault(candidate => candidate.Value == normalized);

        return language is not null;
    }

    public override string ToString()
    {
        return Value;
    }
}
