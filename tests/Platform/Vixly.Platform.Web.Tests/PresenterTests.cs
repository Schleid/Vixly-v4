using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: PresenterTests.cs
//
// PURPOSE:
// Proves the shared presenter behaviour with no Blazor host
// (UI-9).
//
// RESPONSIBILITIES:
// - Every use case starts with a unit-of-work reset and the
//   signed-in context
// - A refused gate becomes access_denied; an unexpected
//   failure becomes ui.error.generic with a reference (S-9)
// - An action panel offers only its own actions, holds the
//   action and its reason through a step-up, and reports what
//   was done
//
// IMPORTANT NOTES:
// - Uses the real GetPlatformUserActions use case over the kit
// =========================================================
public sealed class PresenterTests
{
    private readonly PresenterTestKit _kit = new();

    [Fact]
    public async Task EveryRun_StartsFresh_WithTheSignedInUser()
    {
        ProbePresenter presenter = new(_kit);
        Guid? seen = null;

        Error? error = await presenter.RunAsync(context =>
        {
            seen = context.PlatformUserId;
            return Task.FromResult<Error?>(null);
        });

        Assert.Null(error);
        Assert.Equal(PresenterTestKit.ActorId, seen);
        Assert.Equal(1, _kit.Discards);
    }

    [Fact]
    public async Task Refusal_AndFailure_BecomeErrorCodes()
    {
        ProbePresenter presenter = new(_kit);

        Error? denied = await presenter.RunAsync(_ => throw new UnauthorizedAccessException("x"));
        Error? failed = await presenter.RunAsync(_ => throw new InvalidOperationException("secret detail"));

        Assert.Equal(PlatformSecurityErrors.AccessDenied, denied);
        Assert.Equal("ui.error.generic", failed!.Code);
        Assert.Equal(presenter.FailureReference, failed.Arguments["reference"]);
        Assert.DoesNotContain("secret", failed.Arguments["reference"].ToString(), StringComparison.Ordinal);
        Assert.False(presenter.IsBusy);
    }

    [Fact]
    public async Task ActionPanel_OffersOnlyItsOwnActions()
    {
        ScriptedActionsPresenter presenter = new(_kit);

        await presenter.LoadAsync(PresenterTestKit.TargetId);

        Assert.Equal(
            new[] { PlatformUserAdminAction.Lock, PlatformUserAdminAction.Suspend, PlatformUserAdminAction.Deactivate },
            presenter.State.Available);
    }

    [Fact]
    public async Task ActionPanel_HoldsTheActionThroughAStepUp_ThenReportsItDone()
    {
        ScriptedActionsPresenter presenter = new(_kit, PlatformCommonErrors.StepUpRequired, null);
        await presenter.LoadAsync(PresenterTestKit.TargetId);
        presenter.Select(PlatformUserAdminAction.Lock);

        bool first = await presenter.SubmitAsync(PresenterTestKit.TargetId, "Laptop stolen");

        Assert.False(first);
        Assert.True(presenter.State.StepUpPending);
        Assert.Equal(PlatformUserAdminAction.Lock, presenter.State.Selected);

        _kit.TargetStatus = PlatformUserAccountStatus.Locked;
        bool resumed = await presenter.ResumeAfterStepUpAsync(PresenterTestKit.TargetId);

        Assert.True(resumed);
        Assert.Equal(new[] { "Laptop stolen", "Laptop stolen" }, presenter.Reasons);
        Assert.Equal(PlatformUserAdminAction.Lock, presenter.State.Done);
        Assert.Null(presenter.State.Selected);
        Assert.False(presenter.State.StepUpPending);
        Assert.Contains(PlatformUserAdminAction.Reactivate, presenter.State.Available);
        Assert.DoesNotContain(PlatformUserAdminAction.Lock, presenter.State.Available);
    }

    [Fact]
    public async Task ActionPanel_ShowsADomainError_AndKeepsTheSelection()
    {
        ScriptedActionsPresenter presenter = new(_kit, PlatformIdentityErrors.ReasonRequired);
        await presenter.LoadAsync(PresenterTestKit.TargetId);
        presenter.Select(PlatformUserAdminAction.Suspend);

        Assert.False(await presenter.SubmitAsync(PresenterTestKit.TargetId, " "));
        Assert.Equal(PlatformIdentityErrors.ReasonRequired, presenter.State.Error);
        Assert.Equal(PlatformUserAdminAction.Suspend, presenter.State.Selected);

        presenter.Cancel();
        Assert.Null(presenter.State.Selected);
        Assert.Null(presenter.State.Error);
    }
}
