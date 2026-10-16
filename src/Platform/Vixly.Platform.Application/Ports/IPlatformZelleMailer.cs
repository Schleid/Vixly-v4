using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformZelleMailer.cs
//
// PURPOSE:
// Sends the Zelle emails to a tenant contact.
//
// RESPONSIBILITIES:
// - Write the email in the tenant's language and send it
//
// IMPORTANT NOTES:
// - Called from the outbox only
// =========================================================
public interface IPlatformZelleMailer
{
    Task SendAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformZelleMail mail, CancellationToken cancellationToken);
}
