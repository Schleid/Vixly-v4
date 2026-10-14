using Vixly.Platform.Application.Security;
using Vixly.Platform.Web.Features;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: ProbePresenter.cs
//
// PURPOSE:
// A minimal presenter that runs whatever work a test gives it.
//
// RESPONSIBILITIES:
// - Expose RunUseCaseAsync to the tests
//
// IMPORTANT NOTES:
// - Test double only
// =========================================================
internal sealed class ProbePresenter : PlatformPresenter<string>
{
    public ProbePresenter(PresenterTestKit kit)
        : base(kit.Scope(), "initial", PresenterTestKit.Logger<ProbePresenter>())
    {
    }

    public Task<Error?> RunAsync(Func<PlatformAuthorizationContext, Task<Error?>> work)
    {
        return RunUseCaseAsync((context, _) => work(context));
    }
}
