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

    // Owner, R-6: a device was blocked after a sign-in from another country; one administrator is told.
    Task SendDeviceBlockedAsync(OperationsDeviceBlockedMail mail, CancellationToken cancellationToken);

    // Owner, 2026-10-29 (ticket desk): a ticket waits too long; to the department's managers.
    Task SendTicketEscalatedAsync(OperationsTicketEscalatedMail mail, CancellationToken cancellationToken);

    // Shipment cap (owner, 2026-10-29).
    Task SendShipmentCapAsync(OperationsShipmentCapMail mail, CancellationToken cancellationToken);
}
