using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Collections.AddPlatformCollectionNote;
using Vixly.Platform.Application.UseCases.Collections.CheckPlatformTenantCollections;
using Vixly.Platform.Application.UseCases.Collections.GetPlatformCollectionCase;
using Vixly.Platform.Application.UseCases.Collections.GrantPlatformCollectionExtension;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Collections;

// =========================================================
// FILE: CollectionCasePresenter.cs
//
// PURPOSE:
// Behaviour of the collections case page (D-C7, D-C8).
//
// RESPONSIBILITIES:
// - Load the case
// - Add a note, check the tenant again now, give an extension
//
// IMPORTANT NOTES:
// - An extension asks for step-up; its reason stays internal
// =========================================================
public sealed class CollectionCasePresenter : PlatformPresenter<CollectionCaseState>
{
    private readonly GetPlatformCollectionCaseUseCase _get;

    private readonly AddPlatformCollectionNoteUseCase _note;

    private readonly CheckPlatformTenantCollectionsUseCase _check;

    private readonly GrantPlatformCollectionExtensionUseCase _extend;

    public CollectionCasePresenter(
        GetPlatformCollectionCaseUseCase get,
        AddPlatformCollectionNoteUseCase note,
        CheckPlatformTenantCollectionsUseCase check,
        GrantPlatformCollectionExtensionUseCase extend,
        PlatformUseCaseScope scope,
        ILogger<CollectionCasePresenter> logger)
        : base(scope, CollectionCaseState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _note = note ?? throw new ArgumentNullException(nameof(note));
        _check = check ?? throw new ArgumentNullException(nameof(check));
        _extend = extend ?? throw new ArgumentNullException(nameof(extend));
    }

    public async Task LoadAsync(Guid caseId)
    {
        GetPlatformCollectionCaseResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformCollectionCaseRequest { CaseId = caseId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Detail = response.Detail, Loaded = true }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public async Task<bool> AddNoteAsync(Guid caseId, string? note)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _note.ExecuteAsync(context, new AddPlatformCollectionNoteRequest { CaseId = caseId, Note = note }, cancellationToken)).Error);
        return await FinishAsync(caseId, error, "ui.collections.note_added");
    }

    public async Task<bool> CheckAsync(Guid caseId, Guid tenantId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _check.ExecuteAsync(context, new CheckPlatformTenantCollectionsRequest { PlatformTenantId = tenantId }, cancellationToken)).Error);
        return await FinishAsync(caseId, error, "ui.collections.checked");
    }

    public async Task<bool> ExtendAsync(Guid caseId, DateTime through, string? reason)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _extend.ExecuteAsync(context, new GrantPlatformCollectionExtensionRequest { CaseId = caseId, Through = through, Reason = reason }, cancellationToken)).Error);
        return await FinishAsync(caseId, error, "ui.collections.extension_granted");
    }

    private async Task<bool> FinishAsync(Guid caseId, Error? error, string doneKey)
    {
        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return false;
        }

        await LoadAsync(caseId);
        SetState(State with { StepUpPending = false, DoneKey = error is null ? doneKey : null, Error = error });
        return error is null;
    }
}
