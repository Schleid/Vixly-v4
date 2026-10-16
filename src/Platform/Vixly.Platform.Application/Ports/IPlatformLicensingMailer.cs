using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformLicensingMailer.cs
//
// PURPOSE:
// Sends the licensing emails in the tenant's language (D-L16).
//
// RESPONSIBILITIES:
// - Write one email for the owner from a prepared mail
//
// IMPORTANT NOTES:
// - Called only by the outbox handler
// =========================================================
public interface IPlatformLicensingMailer
{
    Task SendAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformLicensingMail mail, CancellationToken cancellationToken);
}
