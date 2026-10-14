using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantRegistrationListItem.cs
//
// PURPOSE:
// One row of the registrations list.
//
// RESPONSIBILITIES:
// - Carry read-only data to the console
//
// IMPORTANT NOTES:
// - Status is as stored; the console shows a passed deadline as
//   expired until the registration is next read
// =========================================================
public sealed record PlatformTenantRegistrationListItem(
    Guid RegistrationId,
    PlatformTenantRegistrationStatus Status,
    string LegalName,
    string OwnerName,
    string OwnerEmail,
    string Slug,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? EmailVerifiedUtc,
    Guid? PlatformTenantId);
