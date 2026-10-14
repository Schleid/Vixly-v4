using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.UseCases.Tenants;

// =========================================================
// FILE: PlatformContactInput.cs
//
// PURPOSE:
// One contact as a form sends it.
//
// RESPONSIBILITIES:
// - Carry role, name, email and optional phone
// - Build PlatformTenantContactDetails, throwing coded errors
//
// IMPORTANT NOTES:
// - A blank phone means none
// =========================================================
public sealed class PlatformContactInput
{
    public PlatformTenantContactRole Role { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public PlatformTenantContactDetails ToDetails()
    {
        return PlatformTenantContactDetails.Create(
            Role,
            Name,
            EmailAddress.Create(Email),
            string.IsNullOrWhiteSpace(Phone) ? null : PhoneNumber.Create(Phone));
    }
}
