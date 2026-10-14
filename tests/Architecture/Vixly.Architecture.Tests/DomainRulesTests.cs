using System.Reflection;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Architecture.Tests;

// =========================================================
// FILE: DomainRulesTests.cs
//
// PURPOSE:
// Enforces entity naming and Domain purity (Global Standard 3
// and 4.1).
//
// RESPONSIBILITIES:
// - Require every concrete entity to carry its plane prefix
// - Keep Domain assemblies free of framework dependencies
//
// IMPORTANT NOTES:
// - Add each new Domain assembly to SourceAssemblies as its
//   slice opens
// =========================================================
public sealed class DomainRulesTests
{
    private static readonly (string Prefix, Assembly Assembly)[] DomainAssemblies =
    {
        ("Platform", typeof(PlatformUserAuthorityLevel).Assembly)
    };

    [Fact]
    public void ConcreteEntities_CarryTheirPlatformPrefix()
    {
        foreach ((string prefix, Assembly assembly) in DomainAssemblies)
        {
            IEnumerable<Type> entities = assembly
                .GetExportedTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false }
                    && type.IsAssignableTo(typeof(AuditableEntity)));

            foreach (Type entity in entities)
            {
                Assert.True(
                    entity.Name.StartsWith(prefix, StringComparison.Ordinal),
                    $"{entity.FullName} must start with '{prefix}' (Global Standard 4.1).");
            }
        }
    }

    [Fact]
    public void DomainAssemblies_ReferenceNoFrameworkOrOtherPlane()
    {
        string[] allowedPrefixes = { "System", "netstandard", "mscorlib", "Vixly.Shared.Kernel" };

        foreach ((_, Assembly assembly) in DomainAssemblies)
        {
            foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
            {
                string name = reference.Name ?? string.Empty;

                Assert.True(
                    allowedPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)),
                    $"{assembly.GetName().Name} references {name}, which Domain may not use.");
            }
        }
    }

    [Fact]
    public void Scanning_ReachesTheExpectedAssemblies()
    {
        Assert.Equal("Vixly.Platform.Domain", DomainAssemblies[0].Assembly.GetName().Name);
        Assert.Equal("Vixly.Platform.Application", typeof(PlatformAuthorizationContext).Assembly.GetName().Name);
    }
}
