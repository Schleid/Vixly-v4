using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformTenantMailer.cs
//
// PURPOSE:
// Sends the emails of tenant management: registration links and
// status notices.
//
// RESPONSIBILITIES:
// - Send a registration verification link in the registrant's
//   language
// - Send a status notice to one tenant contact in the tenant's
//   language
//
// IMPORTANT NOTES:
// - These are Vixly emails: Control is Vixly's (ADR-0012)
// - A notice names the status and, for a suspension, the category;
//   it never carries the internal reason (D-T18)
// =========================================================
public interface IPlatformTenantMailer
{
    Task SendRegistrationLinkAsync(PlatformTenantRegistration registration, string token, CancellationToken cancellationToken);

    Task SendStatusNoticeAsync(
        PlatformTenant tenant,
        PlatformTenantContact recipient,
        PlatformTenantStatus status,
        PlatformSuspensionCategory? category,
        CancellationToken cancellationToken);

    // B12: staff who review changes hear of a new request; the tenant owner hears the decision.
    Task SendCompanyChangeSubmittedAsync(
        Vixly.Platform.Application.Queries.PlatformPermissionHolderItem recipient,
        PlatformTenant tenant,
        PlatformTenantChangeRequest request,
        CancellationToken cancellationToken);

    Task SendCompanyChangeDecidedAsync(
        PlatformTenant tenant,
        PlatformTenantContact recipient,
        PlatformTenantChangeRequest request,
        CancellationToken cancellationToken);
}
