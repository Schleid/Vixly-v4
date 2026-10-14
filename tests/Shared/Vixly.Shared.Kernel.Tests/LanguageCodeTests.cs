using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Shared.Kernel.Tests;

// =========================================================
// FILE: LanguageCodeTests.cs
//
// PURPOSE:
// Verifies the supported-language value object (ADR-0005).
//
// RESPONSIBILITIES:
// - Cover normalization of regional and mixed-case input
// - Cover rejection of unsupported languages
// - Pin the supported set and the default
//
// IMPORTANT NOTES:
// - Changing the supported set needs an ADR; this test is
//   the tripwire
// =========================================================
public sealed class LanguageCodeTests
{
    [Fact]
    public void SupportedSet_IsEnglishFrenchSpanish_WithEnglishAsDefault()
    {
        Assert.Equal("en,fr,es", string.Join(",", LanguageCode.All.Select(language => language.Value)));
        Assert.Equal(LanguageCode.En, LanguageCode.Default);
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("FR", "fr")]
    [InlineData(" es ", "es")]
    [InlineData("fr-CA", "fr")]
    [InlineData("es_MX", "es")]
    [InlineData("en-US", "en")]
    public void TryCreate_NormalizesToTheNeutralCode(string input, string expected)
    {
        Assert.True(LanguageCode.TryCreate(input, out LanguageCode? language));
        Assert.Equal(expected, language!.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("de")]
    [InlineData("pt-BR")]
    [InlineData("-fr")]
    [InlineData("english")]
    public void TryCreate_RejectsUnsupportedLanguages(string? input)
    {
        Assert.False(LanguageCode.TryCreate(input, out LanguageCode? language));
        Assert.Null(language);
    }

    [Fact]
    public void Create_ThrowsTheUnsupportedLanguageError()
    {
        DomainException exception = Assert.Throws<DomainException>(() => LanguageCode.Create("de"));

        Assert.Equal(KernelErrors.LanguageUnsupported, exception.Error);
    }
}
