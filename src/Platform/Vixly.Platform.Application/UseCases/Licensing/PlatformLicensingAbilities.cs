namespace Vixly.Platform.Application.UseCases.Licensing;

// =========================================================
// FILE: PlatformLicensingAbilities.cs
//
// PURPOSE:
// What the reader may do in the licensing screens (D-L13).
//
// RESPONSIBILITIES:
// - Tell the page which actions to show
//
// IMPORTANT NOTES:
// - The use cases check again
// =========================================================
public sealed record PlatformLicensingAbilities(bool CanManage, bool CanDecideReleases)
{
    public static PlatformLicensingAbilities None { get; } = new(false, false);
}
