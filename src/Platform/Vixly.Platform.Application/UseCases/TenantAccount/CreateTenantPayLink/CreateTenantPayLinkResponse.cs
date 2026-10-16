using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.CreateTenantPayLink;

// =========================================================
// FILE: CreateTenantPayLinkResponse.cs
//
// PURPOSE:
// A payment link for one of the tenant's open invoices (call 23).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreateTenantPayLinkResponse
{
    private CreateTenantPayLinkResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string Url { get; private init; } = string.Empty;

    public DateTimeOffset ExpiresUtc { get; private init; }

    public static CreateTenantPayLinkResponse Success(string url, DateTimeOffset expiresUtc)
    {
        return new CreateTenantPayLinkResponse { Succeeded = true, Url = url, ExpiresUtc = expiresUtc };
    }

    public static CreateTenantPayLinkResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreateTenantPayLinkResponse { Error = error };
    }
}
