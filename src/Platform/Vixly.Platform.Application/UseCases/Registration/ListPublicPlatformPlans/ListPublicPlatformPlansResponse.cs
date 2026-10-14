using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registration.ListPublicPlatformPlans;

// =========================================================
// FILE: ListPublicPlatformPlansResponse.cs
//
// PURPOSE:
// Lists the public plans for the registration catalogue.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPublicPlatformPlansResponse
{
    private ListPublicPlatformPlansResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformPublicPlanItem> Plans { get; private init; } = Array.Empty<PlatformPublicPlanItem>();

    public static ListPublicPlatformPlansResponse Success(IReadOnlyList<PlatformPublicPlanItem> plans)
    {
        return new ListPublicPlatformPlansResponse { Succeeded = true, Plans = plans };
    }

    public static ListPublicPlatformPlansResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPublicPlatformPlansResponse { Error = error };
    }
}
