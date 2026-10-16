using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalRegister;

// =========================================================
// FILE: GetPlatformLegalRegisterResponse.cs
//
// PURPOSE:
// Pages the tenants that must accept the current required Terms, with their status (D-Lg12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformLegalRegisterResponse
{
    private GetPlatformLegalRegisterResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformLegalRegisterItem>? Tenants { get; private init; }

    public PlatformLegalDocumentItem? Required { get; private init; }

    public static GetPlatformLegalRegisterResponse Success(PagedResult<PlatformLegalRegisterItem>? tenants, PlatformLegalDocumentItem? required)
    {
        return new GetPlatformLegalRegisterResponse { Succeeded = true, Tenants = tenants, Required = required };
    }

    public static GetPlatformLegalRegisterResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformLegalRegisterResponse { Error = error };
    }
}
