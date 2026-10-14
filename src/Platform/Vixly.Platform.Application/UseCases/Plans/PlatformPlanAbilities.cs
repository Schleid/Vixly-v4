namespace Vixly.Platform.Application.UseCases.Plans;

// =========================================================
// FILE: PlatformPlanAbilities.cs
//
// PURPOSE:
// What the reader may do on the plans screens.
//
// RESPONSIBILITIES:
// - Say whether the reader may manage plans and publish versions
//
// IMPORTANT NOTES:
// - Used to hide actions; every use case still checks (ADR-0001)
// =========================================================
public sealed record PlatformPlanAbilities(bool CanManage, bool CanPublish, bool CanDecide = false)
{
    public static PlatformPlanAbilities None { get; } = new(false, false);
}
