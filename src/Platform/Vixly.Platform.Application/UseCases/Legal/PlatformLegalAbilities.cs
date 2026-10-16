namespace Vixly.Platform.Application.UseCases.Legal;

// =========================================================
// FILE: PlatformLegalAbilities.cs
//
// PURPOSE:
// What the reader may do in the legal screens (D-Lg13).
//
// RESPONSIBILITIES:
// - Tell the page which actions to show
//
// IMPORTANT NOTES:
// - The use cases check again
// =========================================================
public sealed record PlatformLegalAbilities(bool CanManage, bool CanPublish, bool CanRemind, bool CanRecord)
{
    public static PlatformLegalAbilities None { get; } = new(false, false, false, false);
}
