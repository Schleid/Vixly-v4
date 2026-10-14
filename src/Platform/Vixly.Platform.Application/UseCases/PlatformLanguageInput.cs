using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases;

// =========================================================
// FILE: PlatformLanguageInput.cs
//
// PURPOSE:
// Reads the reader language a request carries.
//
// RESPONSIBILITIES:
// - Turn an optional language code into a supported language, English when absent or unknown
//
// IMPORTANT NOTES:
// - Used by use cases that return translated plan names
// =========================================================
public static class PlatformLanguageInput
{
    public static LanguageCode Read(string? value)
    {
        return LanguageCode.TryCreate(value, out LanguageCode? language) ? language : LanguageCode.Default;
    }
}
