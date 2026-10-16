using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Collections.GetPlatformCollectionCase;

// =========================================================
// FILE: GetPlatformCollectionCaseUseCase.cs
//
// PURPOSE:
// Reads a collections case: its overdue invoices, history and the actions the reader may take.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformCollectionCaseUseCase
{
    private readonly PlatformCollectionsAdministration _collections;

    public GetPlatformCollectionCaseUseCase(
        PlatformCollectionsAdministration collections)
    {
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }

    public async Task<GetPlatformCollectionCaseResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformCollectionCaseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _collections.AuthorizeAsync(context, PlatformPermissions.CollectionsView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformCollectionCaseResponse.Failure(refused);
        }

        PlatformCollectionCase? collectionCase = await _collections.Store.GetCaseAsync(request.CaseId, cancellationToken);
        PlatformCollectionCaseItem? item = collectionCase is null ? null : await _collections.Queries.GetCaseAsync(collectionCase.Id, _collections.UtcNow, cancellationToken);

        if (collectionCase is null || item is null)
        {
            return GetPlatformCollectionCaseResponse.Failure(PlatformCollectionsErrors.CaseNotFound);
        }

        return GetPlatformCollectionCaseResponse.Success(new PlatformCollectionCaseDetail(
            item,
            collectionCase.IsOpen ? await _collections.Queries.ListOverdueInvoicesAsync(collectionCase.PlatformTenantId, _collections.UtcNow, cancellationToken) : Array.Empty<PlatformOverdueInvoiceItem>(),
            await _collections.Queries.ListEventsAsync(collectionCase.Id, cancellationToken),
            await PlatformCollectionsAbilities.ForAsync(_collections, context, collectionCase, cancellationToken)));
    }
}
