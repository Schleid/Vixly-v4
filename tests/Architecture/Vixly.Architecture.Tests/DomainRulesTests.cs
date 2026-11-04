using System.Reflection;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Jobs;
using Vixly.Operations.Domain.Messaging;
using Vixly.Operations.Domain.Platform;
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
        ("Platform", typeof(PlatformUserAuthorityLevel).Assembly),
        ("Operations", typeof(OperationsEntity).Assembly)
    };

    // The only Operations tables without a tenant (Operations Slice 0, D-F1). Adding one needs a design decision.
    private static readonly Type[] OperationsSystemEntities =
    {
        typeof(OperationsOutboxMessage),
        typeof(OperationsJobRun),
        typeof(OperationsPlatformFeedPosition)
    };

    [Fact]
    public void OperationsEntities_CarryATenant_UnlessListedAsSystem()
    {
        Type[] entities = typeof(OperationsEntity).Assembly
            .GetExportedTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(AuditableEntity)))
            .ToArray();

        foreach (Type entity in entities)
        {
            bool tenant = entity.IsAssignableTo(typeof(OperationsEntity));
            bool system = entity.IsAssignableTo(typeof(IOperationsSystemEntity));

            Assert.True(tenant != system, $"{entity.FullName} must either derive from OperationsEntity or be a listed system entity (D-F1).");
            Assert.True(!system || OperationsSystemEntities.Contains(entity), $"{entity.FullName} is a system entity that is not on the D-F1 list.");
        }

        Assert.Equal(OperationsSystemEntities.Length, entities.Count(entity => entity.IsAssignableTo(typeof(IOperationsSystemEntity))));
    }

    [Fact]
    public void ConcreteEntities_CarryTheirPlanePrefix()
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
        Assert.Equal("Vixly.Operations.Domain", DomainAssemblies[1].Assembly.GetName().Name);
        Assert.Equal("Vixly.Operations.Application", typeof(OperationsAuthorizationContext).Assembly.GetName().Name);
    }
}
