using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: FakeIdentityMailer.cs
//
// PURPOSE:
// Keeps the staff emails the host would send.
//
// RESPONSIBILITIES:
// - Record each invitation
//
// IMPORTANT NOTES:
// - Tests only
// =========================================================
internal sealed class FakeIdentityMailer : IOperationsIdentityMailer
{
    public List<OperationsOwnerInvitationMail> Invitations { get; } = new();

    public List<OperationsPasswordResetMail> Resets { get; } = new();

    public Task SendPasswordResetAsync(OperationsPasswordResetMail mail, CancellationToken cancellationToken)
    {
        Resets.Add(mail);
        return Task.CompletedTask;
    }

    public Task SendOwnerInvitationAsync(OperationsOwnerInvitationMail mail, CancellationToken cancellationToken)
    {
        Invitations.Add(mail);
        return Task.CompletedTask;
    }
}
