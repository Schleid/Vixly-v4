using System.Reflection;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Architecture.Tests;

// =========================================================
// FILE: LocalizationRulesTests.cs
//
// PURPOSE:
// Enforces the localization rules of Global Standard 5.5 that
// can be checked from compiled code.
//
// RESPONSIBILITIES:
// - Keep Domain and Application free of resource and
//   localization assemblies
// - Require DomainException to take an Error, never text
// - Validate every declared Error: code format, uniqueness and
//   the plane prefix of its assembly
//
// IMPORTANT NOTES:
// - Resource completeness (en, fr, es keys, placeholders,
//   plural categories) and the no-literal-text-in-markup rule
//   are added when the first Resources project and the first
//   Razor project exist
// - Add each new Domain or Application assembly here as its
//   slice opens
// =========================================================
public sealed class LocalizationRulesTests
{
    private static readonly (string Prefix, Assembly Assembly)[] ErrorAssemblies =
    {
        ("shared", typeof(DomainException).Assembly),
        ("platform", typeof(PlatformSecurityErrors).Assembly),
        ("platform", typeof(PlatformUserAuthorityLevel).Assembly)
    };

    private static readonly Assembly[] BusinessAssemblies =
    {
        typeof(PlatformUserAuthorityLevel).Assembly,
        typeof(PlatformAuthorizationContext).Assembly
    };

    [Fact]
    public void BusinessAssemblies_ReferenceNoResourceOrLocalizationAssembly()
    {
        foreach (Assembly assembly in BusinessAssemblies)
        {
            foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
            {
                string name = reference.Name ?? string.Empty;

                Assert.False(
                    name.EndsWith(".Resources", StringComparison.Ordinal)
                    || name == "Vixly.Shared.Localization"
                    || name.StartsWith("Microsoft.Extensions.Localization", StringComparison.Ordinal),
                    $"{assembly.GetName().Name} references {name}: user-facing text belongs in the presentation layer (5.5).");
            }
        }
    }

    [Fact]
    public void DomainException_AcceptsOnlyAnError()
    {
        ConstructorInfo[] constructors = typeof(DomainException).GetConstructors();

        Assert.True(constructors.Length == 1, "DomainException must have exactly one public constructor.");

        ParameterInfo[] parameters = constructors[0].GetParameters();

        Assert.True(
            parameters.Length == 1 && parameters[0].ParameterType == typeof(Error),
            "DomainException must take a single Error, never a message string.");
    }

    [Fact]
    public void DeclaredErrors_AreWellFormed_UniqueAndPrefixedByTheirAssembly()
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        int count = 0;

        foreach ((string prefix, Assembly assembly) in ErrorAssemblies)
        {
            IEnumerable<Type> catalogues = assembly
                .GetExportedTypes()
                .Where(type => type.IsAbstract && type.IsSealed && type.Name.EndsWith("Errors", StringComparison.Ordinal));

            foreach (Type catalogue in catalogues)
            {
                foreach (PropertyInfo property in catalogue
                    .GetProperties(BindingFlags.Public | BindingFlags.Static)
                    .Where(candidate => candidate.PropertyType == typeof(Error)))
                {
                    Error error = (Error)property.GetValue(null)!;
                    count++;

                    Assert.True(
                        error.Code.StartsWith(prefix + ".", StringComparison.Ordinal),
                        $"{catalogue.Name}.{property.Name} code '{error.Code}' must start with '{prefix}.'.");
                    Assert.True(
                        seen.Add(error.Code),
                        $"Error code '{error.Code}' is declared more than once.");
                }
            }
        }

        Assert.True(count > 0, "No declared errors were found; the scan is not reaching the catalogues.");
    }
}
