using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Web.Features.Users;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: ScriptedActionsPresenter.cs
//
// PURPOSE:
// An action panel presenter whose actions answer from a script,
// to test the shared panel behaviour.
//
// RESPONSIBILITIES:
// - Handle the account status actions
// - Answer each run with the next scripted Error (null = done)
//   and record the reason it was given
//
// IMPORTANT NOTES:
// - Test double only
// =========================================================
internal sealed class ScriptedActionsPresenter : UserActionsPresenter
{
    private readonly Queue<Error?> _answers;

    public ScriptedActionsPresenter(PresenterTestKit kit, params Error?[] answers)
        : base(kit.ActionsUseCase(), kit.Scope(), PresenterTestKit.Logger<ScriptedActionsPresenter>())
    {
        _answers = new Queue<Error?>(answers);
    }

    public List<string> Reasons { get; } = new();

    protected override IReadOnlyCollection<PlatformUserAdminAction> Handles { get; } = new[]
    {
        PlatformUserAdminAction.Lock,
        PlatformUserAdminAction.Suspend,
        PlatformUserAdminAction.Deactivate,
        PlatformUserAdminAction.Reactivate
    };

    protected override Task<Error?> RunActionAsync(
        PlatformUserAdminAction action,
        PlatformAuthorizationContext context,
        Guid platformUserId,
        string reason,
        CancellationToken cancellationToken)
    {
        Reasons.Add(reason);

        return Task.FromResult(_answers.Dequeue());
    }
}
