using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registration.SelectRegistrationPlan;

// =========================================================
// FILE: SelectRegistrationPlanResponse.cs
//
// PURPOSE:
// Records the plan a registering company chooses, and starts its subscription, with an offer link or a promotion code (D-S12, D-S20, D-S21).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SelectRegistrationPlanResponse
{
    private SelectRegistrationPlanResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantRegistrationStatus Step { get; private init; }

    public DateTimeOffset? TrialEndsUtc { get; private init; }

    public static SelectRegistrationPlanResponse Success(PlatformTenantRegistrationStatus step, DateTimeOffset? trialEndsUtc)
    {
        return new SelectRegistrationPlanResponse { Succeeded = true, Step = step, TrialEndsUtc = trialEndsUtc };
    }

    public static SelectRegistrationPlanResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SelectRegistrationPlanResponse { Error = error };
    }
}
