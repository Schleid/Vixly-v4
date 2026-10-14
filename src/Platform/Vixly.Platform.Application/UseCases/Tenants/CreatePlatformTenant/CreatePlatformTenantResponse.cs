using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.CreatePlatformTenant;

// =========================================================
// FILE: CreatePlatformTenantResponse.cs
//
// PURPOSE:
// Result of CreatePlatformTenant.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreatePlatformTenantResponse
{
    private CreatePlatformTenantResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PlatformTenantId { get; private init; }

    public string TenantCode { get; private init; } = string.Empty;

    public static CreatePlatformTenantResponse Success(Guid platformTenantId, string tenantCode)
    {
        return new CreatePlatformTenantResponse { Succeeded = true, PlatformTenantId = platformTenantId, TenantCode = tenantCode };
    }

    public static CreatePlatformTenantResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreatePlatformTenantResponse { Error = error };
    }
}
