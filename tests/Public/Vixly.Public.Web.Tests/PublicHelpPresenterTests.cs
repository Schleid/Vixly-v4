using Microsoft.Extensions.Logging.Abstractions;
using Vixly.Public.Web.Features.Help;

namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: PublicHelpPresenterTests.cs
//
// PURPOSE:
// Verifies help browsing state without a Blazor host.
//
// RESPONSIBILITIES:
// - Keep query and not-found handling deterministic.
//
// IMPORTANT NOTES:
// - Reads only embedded published help; no Platform calls.
// =========================================================
public sealed class PublicHelpPresenterTests
{
    [Fact]
    public async Task SearchIgnoresAccentsAndSwitchingToAnArticleClearsResults()
    {
        using PublicHelpPresenter presenter = new(new PublicHelpCatalogue(), NullLogger<PublicHelpPresenter>.Instance);
        await presenter.LoadAsync("fr", null, "coordonnees");
        Assert.NotEmpty(presenter.State.Articles);
        await presenter.LoadAsync("fr", "confirm-your-email", null);
        Assert.Equal("confirm-your-email", presenter.State.Article?.Metadata.Slug);
        Assert.Empty(presenter.State.Articles);
        await presenter.LoadAsync("fr", "unknown", null);
        Assert.True(presenter.State.NotFound);
    }

    [Fact]
    public async Task QueryIsTrimmedBoundedAndCannotExposeSourceMetadata()
    {
        using PublicHelpPresenter presenter = new(new PublicHelpCatalogue(), NullLogger<PublicHelpPresenter>.Instance);
        await presenter.LoadAsync("en", null, "  " + new string('x', 200) + "  ");
        Assert.Equal(160, presenter.State.Query.Length);
        await presenter.LoadAsync("en", null, "RegistrationStart.razor");
        Assert.Empty(presenter.State.Articles);
    }
}
