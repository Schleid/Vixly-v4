using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Plans;

// =========================================================
// FILE: PlatformPlanTarget.cs
//
// PURPOSE:
// A plan loaded for a command, or the reason it was refused.
//
// RESPONSIBILITIES:
// - Carry the plan or the error
//
// IMPORTANT NOTES:
// - Returned by PlatformPlanAdministration.LoadPlanAsync
// =========================================================
public sealed class PlatformPlanTarget
{
    private PlatformPlanTarget(PlatformPlan? plan, Error? error)
    {
        Plan = plan;
        Error = error;
    }

    public PlatformPlan? Plan { get; }

    public Error? Error { get; }

    public static PlatformPlanTarget Found(PlatformPlan plan)
    {
        return new PlatformPlanTarget(plan, null);
    }

    public static PlatformPlanTarget Refused(Error error)
    {
        return new PlatformPlanTarget(null, error);
    }
}
