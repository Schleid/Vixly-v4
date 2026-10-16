using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Collections.GrantPlatformCollectionExtension;

// =========================================================
// FILE: GrantPlatformCollectionExtensionUseCase.cs
//
// PURPOSE:
// Gives a tenant more time: reminders pause and a block is lifted until the date (D-C7).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GrantPlatformCollectionExtensionUseCase
{
    private readonly PlatformCollectionsAdministration _collections;

    public GrantPlatformCollectionExtensionUseCase(
        PlatformCollectionsAdministration collections)
    {
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }

    public async Task<GrantPlatformCollectionExtensionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GrantPlatformCollectionExtensionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // The holder of the higher permission (sales manager) may extend without the ordinary one (D-C7).
        bool holdsBeyond = await _collections.HoldsAsync(context, PlatformPermissions.CollectionsExtendBeyondLimit, cancellationToken);

        if (await _collections.AuthorizeAsync(context, holdsBeyond ? PlatformPermissions.CollectionsExtendBeyondLimit : PlatformPermissions.CollectionsExtend, requireStepUp: true, cancellationToken) is { } refused)
        {
            return GrantPlatformCollectionExtensionResponse.Failure(refused);
        }

        PlatformCollectionCase? collectionCase = await _collections.Store.GetCaseAsync(request.CaseId, cancellationToken);

        if (collectionCase is null)
        {
            return GrantPlatformCollectionExtensionResponse.Failure(PlatformCollectionsErrors.CaseNotFound);
        }

        PlatformCollectionsSettings settings = await _collections.SettingsAsync(cancellationToken);
        bool beyondLimit = collectionCase.ExtensionsUsed >= settings.ExtensionsPerCase;

        // Past the per-case limit only the holder of the higher permission may extend (D-C7).
        if (beyondLimit && !holdsBeyond)
        {
            return GrantPlatformCollectionExtensionResponse.Failure(PlatformCollectionsErrors.ExtensionLimit(settings.ExtensionsPerCase));
        }

        DateTimeOffset utcNow = _collections.UtcNow;
        PlatformCollectionExtension extension;

        try
        {
            extension = collectionCase.GrantExtension(new DateTimeOffset(DateTime.SpecifyKind(request.Through, DateTimeKind.Utc)), request.Reason, beyondLimit, settings, context.PlatformUserId, utcNow);
        }
        catch (DomainException exception)
        {
            return GrantPlatformCollectionExtensionResponse.Failure(exception.Error);
        }

        _collections.Store.AddEvents(new[] { extension.Event });

        if (extension.LiftedBlock
            && await _collections.Subscriptions.Subscriptions.GetCurrentAsync(collectionCase.PlatformTenantId, cancellationToken) is { } subscription
            && subscription.LiftBlockForExtension(collectionCase.ExtendedThrough!.Value, context.PlatformUserId, utcNow) is { } lifted)
        {
            await _collections.RecordSubscriptionAsync(context, subscription, lifted, PlatformPermissions.CollectionsExtend, "platform.subscriptions.block_lifted", cancellationToken);
        }

        _collections.Notice(collectionCase.PlatformTenantId, collectionCase.Id, PlatformCollectionsNoticeKind.ExtensionGranted, extendedThrough: collectionCase.ExtendedThrough);
        _collections.Audit(context, "platform.collections.extension_granted", beyondLimit ? PlatformPermissions.CollectionsExtendBeyondLimit : PlatformPermissions.CollectionsExtend,
            nameof(PlatformTenant), collectionCase.PlatformTenantId, new { through = collectionCase.ExtendedThrough, count = collectionCase.ExtensionsUsed, liftedBlock = extension.LiftedBlock }, request.Reason?.Trim());

        return await _collections.SaveAsync(cancellationToken)
            ? GrantPlatformCollectionExtensionResponse.Success()
            : GrantPlatformCollectionExtensionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
