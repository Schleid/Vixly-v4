namespace Vixly.Platform.Application.UseCases.Plans.RejectPlatformPlanVersion;

// =========================================================
// FILE: RejectPlatformPlanVersionRequest.cs
//
// PURPOSE:
// Sends a submitted plan version back to sales with a reason (D-S26).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RejectPlatformPlanVersionRequest
{
    public Guid PlatformPlanId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
