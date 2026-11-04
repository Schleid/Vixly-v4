﻿using System.Text.RegularExpressions;

namespace Vixly.Architecture.Tests;

// =========================================================
// FILE: DoubleClickRulesTests.cs
//
// PURPOSE:
// Enforces double-click protection (ADR-0019, UI-12).
//
// RESPONSIBILITIES:
// - Every live console component that handles a click or a
//   submit inherits a guarded base (PresenterComponent,
//   PlatformPresenterComponent or GuardedComponent)
// - Every static page shell that posts plain forms loads
//   vixly-forms.js
//
// IMPORTANT NOTES:
// - Source checks, so the test needs no reference to the Web
//   projects
// - VxButton guards its own clicks; the component that owns the
//   handler still needs the guarded base for submits
// =========================================================
public sealed class DoubleClickRulesTests
{
    private static readonly Regex Handler = new(@"@onclick\s*=|@onsubmit\s*=|\bOnValidSubmit\s*=|\bOnSubmit\s*=", RegexOptions.CultureInvariant);

    private static readonly Regex GuardedBase = new(@"^@inherits\s+(\S+\.)?(PlatformPresenterComponent|PresenterComponent|GuardedComponent)\b", RegexOptions.CultureInvariant | RegexOptions.Multiline);

    public static TheoryData<string> InteractiveMarkup()
    {
        TheoryData<string> data = new();
        string[] roots =
        {
            Path.Combine(RepositoryLocator.Root, "src", "Platform", "Vixly.Platform.Web"),
            Path.Combine(RepositoryLocator.Root, "src", "Operations", "Vixly.Operations.Web"),
            Path.Combine(RepositoryLocator.Root, "src", "Customer", "Vixly.Customer.Web")
        };

        foreach (string path in roots.SelectMany(root => Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories))
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                     .Where(path => Handler.IsMatch(File.ReadAllText(path))))
        {
            data.Add(Path.GetRelativePath(RepositoryLocator.Root, path));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(InteractiveMarkup))]
    public void ClickAndSubmitHandlers_LiveInAGuardedComponent(string relativePath)
    {
        string text = File.ReadAllText(Path.Combine(RepositoryLocator.Root, relativePath));

        Assert.True(GuardedBase.IsMatch(text), $"{relativePath} handles clicks or submits but does not inherit a guarded component base (ADR-0019).");
    }

    [Theory]
    [InlineData("src/Platform/Vixly.Platform.Web/Components/SignIn/AuthShell.razor")]
    [InlineData("src/Public/Vixly.Public.Web/Components/App.razor")]
    [InlineData("src/Operations/Vixly.Operations.Web/Components/App.razor")]
    [InlineData("src/Customer/Vixly.Customer.Web/Components/App.razor")]
    public void StaticShells_LoadTheFormGuard(string relativePath)
    {
        string text = File.ReadAllText(Path.Combine(RepositoryLocator.Root, relativePath));

        Assert.Contains("vixly-forms.js", text, StringComparison.Ordinal);
    }
}
