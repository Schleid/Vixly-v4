using System.Text.RegularExpressions;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsSiteText.cs
//
// PURPOSE:
// One text of a tenant site in one language (Slice 1B).
//
// RESPONSIBILITIES:
// - Hold stage, language, key and plain text
//
// IMPORTANT NOTES:
// - Plain text only: angle brackets are refused, and every page encodes output
// =========================================================
public sealed class OperationsSiteText : OperationsEntity
{
    private static readonly Regex Markup = new("[<>]", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private OperationsSiteText()
    {
        Language = string.Empty;
        Key = string.Empty;
        Value = string.Empty;
    }

    internal OperationsSiteText(Guid tenantId, Guid brandId, OperationsSiteTextStage stage, string language, string key, string value, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        BrandId = brandId;
        Stage = stage;
        Language = language;
        Key = key;
        Value = value;
    }

    public Guid BrandId { get; private set; }

    public OperationsSiteTextStage Stage { get; private set; }

    public string Language { get; private set; }

    public string Key { get; private set; }

    public string Value { get; private set; }

    internal static string Check(string language, string key, string value)
    {
        if (!LanguageCode.TryCreate(language, out _))
        {
            throw new DomainException(OperationsBrandingErrors.LanguageInvalid);
        }

        int max = OperationsSiteTextKeys.MaxLength(key) ?? throw new DomainException(OperationsBrandingErrors.TextKeyInvalid);

        if (value.Length > max)
        {
            throw new DomainException(OperationsBrandingErrors.TextTooLong);
        }

        return Markup.IsMatch(value) ? throw new DomainException(OperationsBrandingErrors.TextNotPlain) : value;
    }

    internal void Change(string value, DateTimeOffset utcNow)
    {
        Value = value;
        MarkAsModified(utcNow);
    }
}
