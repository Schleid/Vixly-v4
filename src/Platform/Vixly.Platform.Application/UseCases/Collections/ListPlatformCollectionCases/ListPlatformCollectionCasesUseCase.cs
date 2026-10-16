using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Collections.ListPlatformCollectionCases;

// =========================================================
// FILE: ListPlatformCollectionCasesUseCase.cs
//
// PURPOSE:
// Lists collections cases, paged, with stage, status, currency and extension filters (D-C14).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListPlatformCollectionCasesUseCase
{
    private readonly PlatformCollectionsAdministration _collections;

    public ListPlatformCollectionCasesUseCase(
        PlatformCollectionsAdministration collections)
    {
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }

    public async Task<ListPlatformCollectionCasesResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformCollectionCasesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _collections.AuthorizeAsync(context, PlatformPermissions.CollectionsView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return ListPlatformCollectionCasesResponse.Failure(refused);
        }

        return ListPlatformCollectionCasesResponse.Success(await _collections.Queries.ListCasesAsync(
            new PlatformCollectionCaseFilter(request.Status, request.Stage, string.IsNullOrWhiteSpace(request.CurrencyCode) ? null : request.CurrencyCode.Trim().ToUpperInvariant(), request.InExtension, request.Search?.Trim()),
            Vixly.Shared.Kernel.Paging.PagedRequest.Create(request.Page, request.PageSize),
            _collections.UtcNow,
            cancellationToken));
    }
}
