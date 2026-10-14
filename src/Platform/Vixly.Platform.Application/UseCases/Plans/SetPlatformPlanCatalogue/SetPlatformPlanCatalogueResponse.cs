using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans.SetPlatformPlanCatalogue;

// =========================================================
// FILE: SetPlatformPlanCatalogueResponse.cs
//
// PURPOSE:
// Sets whether a plan shows in the public catalogue, and its order.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetPlatformPlanCatalogueResponse
{
    private SetPlatformPlanCatalogueResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetPlatformPlanCatalogueResponse Success()
    {
        return new SetPlatformPlanCatalogueResponse { Succeeded = true };
    }

    public static SetPlatformPlanCatalogueResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetPlatformPlanCatalogueResponse { Error = error };
    }
}
