using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformLegalMailer.cs
//
// PURPOSE:
// Sends the Terms notices and reminders to a tenant's owner.
//
// RESPONSIBILITIES:
// - Write the email in the tenant's language and send it
//
// IMPORTANT NOTES:
// - Called from the outbox only
// =========================================================
public interface IPlatformLegalMailer
{
    Task SendAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformLegalMail mail, CancellationToken cancellationToken);
}
