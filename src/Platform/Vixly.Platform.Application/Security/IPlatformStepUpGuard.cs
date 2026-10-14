namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: IPlatformStepUpGuard.cs
//
// PURPOSE:
// Checks that the caller confirmed a second factor recently
// (ADR-0006 section 5).
//
// RESPONSIBILITIES:
// - Answer whether the caller's session confirmed a second
//   factor within the step-up window
//
// IMPORTANT NOTES:
// - A context without a session never passes
// =========================================================
public interface IPlatformStepUpGuard
{
    Task<bool> HasRecentSecondFactorAsync(
        PlatformAuthorizationContext context,
        CancellationToken cancellationToken);
}
