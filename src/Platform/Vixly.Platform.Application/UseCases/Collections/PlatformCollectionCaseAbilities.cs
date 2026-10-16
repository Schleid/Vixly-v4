namespace Vixly.Platform.Application.UseCases.Collections;

// =========================================================
// FILE: PlatformCollectionCaseAbilities.cs
//
// PURPOSE:
// What the reader may do on a collections case (D-C7, D-C8).
//
// RESPONSIBILITIES:
// - Tell the page which actions to show and the latest extension date allowed
//
// IMPORTANT NOTES:
// - The use cases check again; this only shapes the page
// =========================================================
public sealed record PlatformCollectionCaseAbilities(
    bool CanManage,
    bool CanExtend,
    bool NeedsBeyondLimit,
    int ExtensionsLeft,
    DateTime LatestExtensionDate)
{
    public static PlatformCollectionCaseAbilities None { get; } = new(false, false, false, 0, DateTime.MinValue);
}
