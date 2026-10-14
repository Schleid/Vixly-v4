namespace Vixly.Platform.Application.UseCases.Sales;

// =========================================================
// FILE: PlatformSalesAbilities.cs
//
// PURPOSE:
// What the current staff member may do with a promotion or an offer.
//
// RESPONSIBILITIES:
// - Say whether they may change it and whether they may decide on it
//
// IMPORTANT NOTES:
// - CanDecide is false for the person who submitted it (four eyes)
// =========================================================
public sealed record PlatformSalesAbilities(bool CanManage, bool CanDecide)
{
    public static PlatformSalesAbilities None { get; } = new(false, false);
}
