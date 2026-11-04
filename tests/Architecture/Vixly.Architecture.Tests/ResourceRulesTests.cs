using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Architecture.Tests;

// =========================================================
// FILE: ResourceRulesTests.cs
//
// PURPOSE:
// Enforces the resource rules of Global Standard 5.5 (L-2,
// L-3, L-10) on every text resource set.
//
// RESPONSIBILITIES:
// - Every resource set has en, fr and es files with the same
//   keys and no empty value
// - Placeholders are named and identical in every language
// - Every declared Error has text in its plane's resource set
//   (platform.* in PlatformText, shared.* in SharedText), and
//   its placeholders are among the Error's argument names
//
// IMPORTANT NOTES:
// - Reads the .resx files, so it needs no satellite
//   assemblies
// - fr and es values commented needs-review pass this build
//   check; the release gate counts them (L-10)
// =========================================================
public sealed class ResourceRulesTests
{
    private static readonly string[] Languages = { "fr", "es" };

    private static readonly Regex Placeholder = new(@"\{([^{}]*)\}", RegexOptions.CultureInvariant);

    private static readonly (string Prefix, string BasePath, Assembly[] Assemblies)[] Sets =
    {
        ("platform", "src/Platform/Vixly.Platform.Resources/PlatformText", new[]
        {
            typeof(PlatformSecurityErrors).Assembly,
            typeof(PlatformUserAuthorityLevel).Assembly
        }),
        ("shared", "src/Shared/Vixly.Shared.Localization/SharedText", new[] { typeof(DomainException).Assembly }),
        ("public", "src/Public/Vixly.Public.Web/Resources/PublicText", Array.Empty<Assembly>()),
        ("ops", "src/Operations/Vixly.Operations.Resources/OperationsText", new[]
        {
            typeof(OperationsInternalErrors).Assembly,
            typeof(OperationsEntity).Assembly
        }),
        ("customer", "src/Customer/Vixly.Customer.Resources/CustomerText", Array.Empty<Assembly>())
    };

    [Theory]
    [InlineData("src/Operations/Vixly.Operations.Resources/OperationsText")]
    [InlineData("src/Customer/Vixly.Customer.Resources/CustomerText")]
    public void TenantTexts_NeverSayVixly(string basePath)
    {
        foreach (string? language in new[] { null, "fr", "es" })
        {
            Assert.All(Read(basePath, language), entry => Assert.DoesNotContain("Vixly", entry.Value, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static TheoryData<string> ResourceSets()
    {
        TheoryData<string> data = new();

        foreach ((_, string basePath, _) in Sets)
        {
            data.Add(basePath);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ResourceSets))]
    public void EveryLanguage_HasTheSameKeys_AndNoEmptyValue(string basePath)
    {
        Dictionary<string, string> english = Read(basePath, null);

        Assert.NotEmpty(english);

        foreach (string language in Languages)
        {
            Dictionary<string, string> translated = Read(basePath, language);

            Assert.Empty(english.Keys.Except(translated.Keys));
            Assert.Empty(translated.Keys.Except(english.Keys));
            Assert.All(translated, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Value), $"{language}: {entry.Key} is empty."));
        }

        Assert.All(english, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Value), $"en: {entry.Key} is empty."));
    }

    [Theory]
    [MemberData(nameof(ResourceSets))]
    public void Placeholders_AreNamed_AndTheSameInEveryLanguage(string basePath)
    {
        Dictionary<string, string> english = Read(basePath, null);

        foreach ((string key, string value) in english)
        {
            string[] names = Names(value);

            Assert.All(names, name => Assert.Matches("^[a-z][A-Za-z0-9]*$", name));

            foreach (string language in Languages)
            {
                Assert.Equal(names, Names(Read(basePath, language)[key]));
            }
        }
    }

    [Fact]
    public void EveryDeclaredError_HasText_WithMatchingPlaceholders()
    {
        int checkedErrors = 0;

        foreach ((string prefix, string basePath, Assembly[] assemblies) in Sets)
        {
            Dictionary<string, string> english = Read(basePath, null);

            foreach (Error error in DeclaredErrors(assemblies))
            {
                checkedErrors++;

                Assert.True(english.TryGetValue(error.Code, out string? text), $"{error.Code} has no text in {basePath}.resx.");
                Assert.StartsWith(prefix + ".", error.Code, StringComparison.Ordinal);
                Assert.All(Names(text!), name => Assert.True(
                    error.Arguments.ContainsKey(name),
                    $"{error.Code}: placeholder {{{name}}} is not an argument of the error."));
            }
        }

        Assert.True(checkedErrors > 50, "The scan is not reaching the error catalogues.");
    }

    private static IEnumerable<Error> DeclaredErrors(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsAbstract && type.IsSealed && type.Name.EndsWith("Errors", StringComparison.Ordinal))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Where(property => property.PropertyType == typeof(Error))
            .Select(property => (Error)property.GetValue(null)!);
    }

    private static string[] Names(string value)
    {
        return Placeholder.Matches(value).Select(match => match.Groups[1].Value).Order(StringComparer.Ordinal).ToArray();
    }

    private static Dictionary<string, string> Read(string basePath, string? language)
    {
        string path = Path.Combine(RepositoryLocator.Root, basePath + (language is null ? string.Empty : "." + language) + ".resx");

        Assert.True(File.Exists(path), $"Missing resource file {path}.");

        return XDocument.Load(path)
            .Descendants("data")
            .ToDictionary(
                data => (string)data.Attribute("name")!,
                data => (string?)data.Element("value") ?? string.Empty,
                StringComparer.Ordinal);
    }
}
