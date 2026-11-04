using Microsoft.Extensions.Logging.Abstractions;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.MyAccount.GetMyPlatformAccess;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Web.Features.Help;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: PlatformHelpPresenterTests.cs
//
// PURPOSE:
// Proves help navigation refreshes the authoritative access snapshot.
//
// RESPONSIBILITIES:
// - Test contextual matching, revocation and failure fallback.
//
// IMPORTANT NOTES:
// - Runs without Blazor or a database (UI-9).
// =========================================================
public sealed class PlatformHelpPresenterTests
{
    [Fact]
    public async Task OlderAccessReadCannotOverwriteANewerNavigation()
    {
        TaskCompletionSource<PlatformPermissionSnapshot?> delayed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        PlatformHelpTestPermissions access = new() { ReadOverride = () => delayed.Task };
        using PlatformHelpPresenter presenter = Create(access);
        Task older = presenter.LoadAsync("en", "superuser-actions", null);
        access.ReadOverride = null;
        await presenter.LoadAsync("en", "company-page", null);
        Assert.True(presenter.State.NotFound);
        delayed.SetResult(new(true, PlatformUserAuthorityLevel.SuperUser, new HashSet<string>()));
        await older;
        Assert.True(presenter.State.NotFound);
        Assert.Null(presenter.State.Article);
    }

    [Fact]
    public async Task StalePrincipalCannotMakeTheReaderSuperUser()
    {
        PlatformHelpTestPermissions access = new();
        using PlatformHelpPresenter presenter = Create(access);
        await presenter.LoadAsync("en", "edit-company-details", null);
        Assert.True(presenter.State.NotFound);
        access.Codes.Add(PlatformPermissions.TenantsEdit);
        await presenter.LoadAsync("en", "edit-company-details", null);
        Assert.False(presenter.State.NotFound);
        access.Codes.Clear();
        await presenter.LoadAsync("en", "edit-company-details", null);
        Assert.True(presenter.State.NotFound);
        Assert.Null(presenter.State.Article);
        Assert.Empty(presenter.State.Related);
    }

    [Fact]
    public async Task ContextMatchesActualRouteWithoutLeakingAnIdentifier()
    {
        PlatformHelpTestPermissions access = new() { Codes = new() { PlatformPermissions.TenantsView } };
        using PlatformHelpPresenter presenter = Create(access);
        await presenter.LoadAsync("fr", null, null, "/tenants/aaaa0000-0000-0000-0000-000000000001");
        Assert.Equal("/help/company-page", presenter.State.ContextAddress);
        access.Codes.Add(PlatformPermissions.TenantsReviewChanges);
        await presenter.LoadAsync("fr", null, null, "/tenants/changes");
        Assert.Equal("/help/company-change-requests", presenter.State.ContextAddress);
        access.Codes.Clear();
        await presenter.LoadAsync("fr", null, null, "/tenants/aaaa0000-0000-0000-0000-000000000001");
        Assert.Equal("/help", presenter.State.ContextAddress);
    }

    [Fact]
    public async Task FailureOrInactiveAccountDropsPreviousRestrictedResults()
    {
        PlatformHelpTestPermissions access = new() { SuperUser = true };
        using PlatformHelpPresenter presenter = Create(access);
        await presenter.LoadAsync("en", null, null);
        Assert.Contains(presenter.State.Articles, article => article.Metadata.Slug == "company-page");
        access.Fails = true;
        await presenter.LoadAsync("en", null, null);
        Assert.All(presenter.State.Articles, article => Assert.Empty(article.Metadata.Permissions));
        Assert.NotNull(presenter.FailureReference);
        access.Fails = false;
        access.Active = false;
        await presenter.LoadAsync("en", null, new string('x', 200));
        Assert.Equal(160, presenter.State.Query.Length);
        Assert.Empty(presenter.State.Articles);
    }

    private static PlatformHelpPresenter Create(PlatformHelpTestPermissions access) => new(new(),
        new GetMyPlatformAccessUseCase(access, TimeProvider.System), new PresenterTestKit().Scope(),
        NullLogger<PlatformHelpPresenter>.Instance);
}
