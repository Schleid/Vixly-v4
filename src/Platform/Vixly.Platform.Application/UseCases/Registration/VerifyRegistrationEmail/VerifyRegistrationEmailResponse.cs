using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registration.VerifyRegistrationEmail;

// =========================================================
// FILE: VerifyRegistrationEmailResponse.cs
//
// PURPOSE:
// Result of VerifyRegistrationEmail.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class VerifyRegistrationEmailResponse
{
    private VerifyRegistrationEmailResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string AccessToken { get; private init; } = string.Empty;

    public string TenantCode { get; private init; } = string.Empty;

    public static VerifyRegistrationEmailResponse Success(string accessToken, string tenantCode)
    {
        return new VerifyRegistrationEmailResponse { Succeeded = true, AccessToken = accessToken, TenantCode = tenantCode };
    }

    public static VerifyRegistrationEmailResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new VerifyRegistrationEmailResponse { Error = error };
    }
}
