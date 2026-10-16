using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ListTenantPlanOptions;

// =========================================================
// FILE: ListTenantPlanOptionsResponse.cs
//
// PURPOSE:
// Plans the tenant may move to by itself: public plans on sale and its current plan (call 20).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListTenantPlanOptionsResponse
{
    private ListTenantPlanOptionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformPlanOption> Options { get; private init; } = Array.Empty<PlatformPlanOption>();

    public static ListTenantPlanOptionsResponse Success(IReadOnlyList<PlatformPlanOption> options)
    {
        return new ListTenantPlanOptionsResponse { Succeeded = true, Options = options };
    }

    public static ListTenantPlanOptionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListTenantPlanOptionsResponse { Error = error };
    }
}
