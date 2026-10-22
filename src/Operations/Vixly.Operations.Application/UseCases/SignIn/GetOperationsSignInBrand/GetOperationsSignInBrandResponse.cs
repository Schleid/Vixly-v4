using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;

// =========================================================
// FILE: GetOperationsSignInBrandResponse.cs
//
// PURPOSE:
// The name a tenant shows on its sign-in page (Slice 1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsSignInBrandResponse
{
    private GetOperationsSignInBrandResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string DisplayName { get; private init; } = string.Empty;

    public static GetOperationsSignInBrandResponse Success(string displayName)
    {
        return new GetOperationsSignInBrandResponse { Succeeded = true, DisplayName = displayName };
    }

    public static GetOperationsSignInBrandResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsSignInBrandResponse { Error = error };
    }
}
