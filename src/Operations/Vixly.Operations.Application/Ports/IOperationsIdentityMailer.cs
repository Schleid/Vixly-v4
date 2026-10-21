namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsIdentityMailer.cs
//
// PURPOSE:
// Sends staff account emails in the recipient language.
//
// RESPONSIBILITIES:
// - Send the owner invitation
//
// IMPORTANT NOTES:
// - Tenant branded: never says "Vixly"
// =========================================================
public interface IOperationsIdentityMailer
{
    Task SendOwnerInvitationAsync(OperationsOwnerInvitationMail mail, CancellationToken cancellationToken);

    Task SendPasswordResetAsync(OperationsPasswordResetMail mail, CancellationToken cancellationToken);
}
