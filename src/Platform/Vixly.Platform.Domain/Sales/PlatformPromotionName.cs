namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformPromotionName.cs
//
// PURPOSE:
// A promotion's name in one language.
//
// RESPONSIBILITIES:
// - Hold a language code and a name
//
// IMPORTANT NOTES:
// - Owned by PlatformPromotion; the English name is required (ADR-0005)
// =========================================================
public sealed class PlatformPromotionName
{
    private PlatformPromotionName()
    {
        Language = string.Empty;
        Name = string.Empty;
    }

    internal PlatformPromotionName(string language, string name)
    {
        Language = language;
        Name = name;
    }

    public string Language { get; private set; }

    public string Name { get; private set; }

    internal void Rename(string name)
    {
        Name = name;
    }
}
