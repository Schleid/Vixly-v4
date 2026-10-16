namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformZelleClaimAbilities.cs
//
// PURPOSE:
// What the reader may do with Zelle reports.
//
// RESPONSIBILITIES:
// - Tell the page whether to show confirm and reject
//
// IMPORTANT NOTES:
// - The use cases check again
// =========================================================
public sealed record PlatformZelleClaimAbilities(bool CanDecide)
{
    public static PlatformZelleClaimAbilities None { get; } = new(false);
}
