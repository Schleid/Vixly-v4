using Vixly.Platform.Application.Collections;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformCollectionsMailer.cs
//
// PURPOSE:
// Sends the collections emails in the tenant's language (D-C11).
//
// RESPONSIBILITIES:
// - Write one email for one recipient from a prepared mail
//
// IMPORTANT NOTES:
// - Called only by the outbox handler
// - No internal reason or note ever goes into an email
// =========================================================
public interface IPlatformCollectionsMailer
{
    Task SendAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformCollectionsMail mail, CancellationToken cancellationToken);
}
