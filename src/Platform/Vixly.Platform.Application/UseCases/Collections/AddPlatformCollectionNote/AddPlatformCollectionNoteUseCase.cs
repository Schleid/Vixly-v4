using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Collections.AddPlatformCollectionNote;

// =========================================================
// FILE: AddPlatformCollectionNoteUseCase.cs
//
// PURPOSE:
// Adds an internal note to a collections case (D-C8).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class AddPlatformCollectionNoteUseCase
{
    private readonly PlatformCollectionsAdministration _collections;

    public AddPlatformCollectionNoteUseCase(
        PlatformCollectionsAdministration collections)
    {
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }

    public async Task<AddPlatformCollectionNoteResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        AddPlatformCollectionNoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _collections.AuthorizeAsync(context, PlatformPermissions.CollectionsManage, requireStepUp: false, cancellationToken) is { } refused)
        {
            return AddPlatformCollectionNoteResponse.Failure(refused);
        }

        PlatformCollectionCase? collectionCase = await _collections.Store.GetCaseAsync(request.CaseId, cancellationToken);

        if (collectionCase is null)
        {
            return AddPlatformCollectionNoteResponse.Failure(PlatformCollectionsErrors.CaseNotFound);
        }

        try
        {
            _collections.Store.AddEvents(new[] { collectionCase.AddNote(request.Note, context.PlatformUserId, _collections.UtcNow) });
        }
        catch (DomainException exception)
        {
            return AddPlatformCollectionNoteResponse.Failure(exception.Error);
        }

        _collections.Audit(context, "platform.collections.note_added", PlatformPermissions.CollectionsManage, nameof(PlatformTenant), collectionCase.PlatformTenantId);

        return await _collections.SaveAsync(cancellationToken)
            ? AddPlatformCollectionNoteResponse.Success()
            : AddPlatformCollectionNoteResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
