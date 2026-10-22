using System.Xml.Linq;

namespace Vixly.Architecture.Tests;

// =========================================================
// FILE: ProjectDependencyRulesTests.cs
//
// PURPOSE:
// Enforces the project dependency rules of Global Standard 3.
//
// RESPONSIBILITIES:
// - Fail when a project references something its kind may not
// - Fail when a project matches no known kind (new kinds need
//   an ADR and a rule here; Shared.Hosting and Worker added by
//   ADR-0011 and ADR-0007)
// - Fail when Domain, Contracts or Kernel take package
//   references
// - Keep user-facing text out of Domain and Application:
//   Resources and Localization projects are referenced only by
//   Infrastructure, Api and Web (Global Standard 5.5)
//
// IMPORTANT NOTES:
// - Reads the .csproj files, so it needs no compiled
//   assemblies for planes that are not built yet
// - Infrastructure may be referenced by Api and Web only for
//   the composition root; that finer rule is checked by the
//   namespace rules added with the first Api project
// =========================================================
public sealed class ProjectDependencyRulesTests
{
    private const string Kernel = "Vixly.Shared.Kernel";
    private const string SharedUi = "Vixly.Shared.UI";
    private const string Localization = "Vixly.Shared.Localization";
    private const string Hosting = "Vixly.Shared.Hosting";
    private const string ContractsPrefix = "Vixly.Contracts.";

    private static readonly string[] Planes = { "Platform", "Operations", "Customer" };

    public static TheoryData<string> AllProjects()
    {
        TheoryData<string> data = new();

        foreach (string path in RepositoryLocator.ProjectFiles())
        {
            data.Add(Path.GetFileNameWithoutExtension(path));
        }

        return data;
    }

    [Fact]
    public void Repository_HasAtLeastTheKernelProject()
    {
        Assert.Contains(
            RepositoryLocator.ProjectFiles(),
            path => Path.GetFileNameWithoutExtension(path) == Kernel);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public void Project_ReferencesOnlyWhatItsKindMayReference(string project)
    {
        Func<string, bool>? isAllowed = AllowedReferences(project);

        Assert.True(
            isAllowed is not null,
            $"{project} matches no known project kind. Add the kind by ADR and extend this test.");

        foreach (string reference in ProjectReferencesOf(project))
        {
            Assert.True(
                isAllowed!(reference),
                $"{project} must not reference {reference} (Global Standard 3).");
        }
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public void BclOnlyProjects_HaveNoPackageReferences(string project)
    {
        bool bclOnly = project == Kernel
            || project.StartsWith(ContractsPrefix, StringComparison.Ordinal)
            || project.EndsWith(".Domain", StringComparison.Ordinal)
            || project.EndsWith(".Resources", StringComparison.Ordinal);

        if (!bclOnly)
        {
            return;
        }

        Assert.Empty(PackageReferencesOf(project));
    }

    private static Func<string, bool>? AllowedReferences(string project)
    {
        if (project == Kernel || project == SharedUi || project.StartsWith(ContractsPrefix, StringComparison.Ordinal))
        {
            return _ => false;
        }

        if (project == Localization || project == Hosting)
        {
            return reference => reference == Kernel;
        }

        if (project == "Vixly.Shared.UI.Gallery")
        {
            return reference => reference == SharedUi;
        }

        if (project == "Vixly.Edge")
        {
            return reference => reference == "Vixly.Contracts.Platform" || reference == Kernel;
        }

        // The dock print program runs on a warehouse computer and only speaks the Operations contracts (Slice 3B).
        if (project == "Vixly.PrintAgent")
        {
            return reference => reference == "Vixly.Contracts.Operations";
        }

        if (project == "Vixly.Public.Web")
        {
            return reference => reference == "Vixly.Contracts.Platform" || reference == SharedUi
                || reference == Localization || reference == Hosting;
        }

        string[] parts = project.Split('.');

        if (parts.Length != 3 || parts[0] != "Vixly" || !Planes.Contains(parts[1]))
        {
            return null;
        }

        string plane = $"Vixly.{parts[1]}";
        string layer = parts[2];

        return layer switch
        {
            "Domain" => reference => reference == Kernel,
            "Application" => reference =>
                reference == $"{plane}.Domain" || reference == Kernel || IsContracts(reference),
            "Resources" => reference => reference == Kernel,
            "Infrastructure" => reference =>
                reference == $"{plane}.Application" || reference == $"{plane}.Domain"
                || reference == Kernel || IsContracts(reference)
                || reference == $"{plane}.Resources" || reference == Localization,
            "Api" => reference =>
                reference == $"{plane}.Application" || reference == $"{plane}.Infrastructure"
                || IsContracts(reference)
                || reference == $"{plane}.Resources" || reference == Localization || reference == Hosting,
            "Web" => reference =>
                reference == $"{plane}.Application" || reference == $"{plane}.Infrastructure"
                || IsContracts(reference) || reference == SharedUi
                || reference == $"{plane}.Resources" || reference == Localization || reference == Hosting,
            "Worker" => reference =>
                reference == $"{plane}.Application" || reference == $"{plane}.Infrastructure"
                || IsContracts(reference)
                || reference == $"{plane}.Resources" || reference == Localization || reference == Hosting,
            _ => null
        };
    }

    private static bool IsContracts(string reference)
    {
        return reference.StartsWith(ContractsPrefix, StringComparison.Ordinal);
    }

    private static string ProjectPath(string project)
    {
        return RepositoryLocator.ProjectFiles()
            .Single(path => Path.GetFileNameWithoutExtension(path) == project);
    }

    private static IEnumerable<string> ProjectReferencesOf(string project)
    {
        return XDocument.Load(ProjectPath(project))
            .Descendants("ProjectReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
            .Select(include => Path.GetFileNameWithoutExtension(
                include.Replace('\\', Path.DirectorySeparatorChar)));
    }

    private static IEnumerable<string> PackageReferencesOf(string project)
    {
        return XDocument.Load(ProjectPath(project))
            .Descendants("PackageReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty);
    }
}
