using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Collections;

namespace Vixly.Platform.Application.UseCases.Collections;

// =========================================================
// FILE: PlatformCollectionsAbilities.cs
//
// PURPOSE:
// Works out what the reader may do on a case from permissions and the settings.
//
// RESPONSIBILITIES:
// - Shape the case page actions
//
// IMPORTANT NOTES:
// - Never a substitute for the use case checks
// =========================================================
public static class PlatformCollectionsAbilities
{
    public static async Task<PlatformCollectionCaseAbilities> ForAsync(
        PlatformCollectionsAdministration collections,
        PlatformAuthorizationContext context,
        PlatformCollectionCase? collectionCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(collections);

        if (collectionCase is not { IsOpen: true })
        {
            return PlatformCollectionCaseAbilities.None;
        }

        PlatformCollectionsSettings settings = await collections.SettingsAsync(cancellationToken);
        bool manage = await collections.HoldsAsync(context, PlatformPermissions.CollectionsManage, cancellationToken);
        bool extend = await collections.HoldsAsync(context, PlatformPermissions.CollectionsExtend, cancellationToken);
        bool beyond = await collections.HoldsAsync(context, PlatformPermissions.CollectionsExtendBeyondLimit, cancellationToken);
        int left = Math.Max(0, settings.ExtensionsPerCase - collectionCase.ExtensionsUsed);
        bool needsBeyond = left == 0;

        return new PlatformCollectionCaseAbilities(
            manage,
            needsBeyond ? beyond : extend,
            needsBeyond,
            left,
            collections.UtcNow.UtcDateTime.Date.AddDays(settings.ExtensionMaxDays));
    }
}
