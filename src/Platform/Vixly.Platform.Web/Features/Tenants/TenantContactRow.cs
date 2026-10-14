using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantContactRow.cs
//
// PURPOSE:
// One editable contact row of the contacts form.
//
// RESPONSIBILITIES:
// - Hold role, name, email and phone while typing
// - Say whether an optional row was left blank
//
// IMPORTANT NOTES:
// - A blank Billing or Technical row means "use the owner"
// =========================================================
public sealed class TenantContactRow
{
    public PlatformTenantContactRole Role { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public bool IsBlank => string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Email);

    public PlatformContactInput ToInput()
    {
        return new PlatformContactInput { Role = Role, Name = Name, Email = Email, Phone = Phone };
    }
}
