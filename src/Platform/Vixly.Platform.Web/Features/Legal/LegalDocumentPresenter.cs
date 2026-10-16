using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal.DeletePlatformLegalDraft;
using Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalDocument;
using Vixly.Platform.Application.UseCases.Legal.PublishPlatformLegalDocument;
using Vixly.Platform.Application.UseCases.Legal.SetPlatformLegalFile;
using Vixly.Platform.Application.UseCases.Legal.UpdatePlatformLegalDraft;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Legal;

// =========================================================
// FILE: LegalDocumentPresenter.cs
//
// PURPOSE:
// Behaviour of the legal document page (Slice 8).
//
// RESPONSIBILITIES:
// - Load the version
// - Save the draft, upload a file per language, publish (step-up), delete the draft
//
// IMPORTANT NOTES:
// - Publishing waits for step-up and runs again once it is done
// =========================================================
public sealed class LegalDocumentPresenter : PlatformPresenter<LegalDocumentState>
{
    private readonly GetPlatformLegalDocumentUseCase _get;

    private readonly UpdatePlatformLegalDraftUseCase _update;

    private readonly SetPlatformLegalFileUseCase _file;

    private readonly PublishPlatformLegalDocumentUseCase _publish;

    private readonly DeletePlatformLegalDraftUseCase _delete;

    public LegalDocumentPresenter(
        GetPlatformLegalDocumentUseCase get,
        UpdatePlatformLegalDraftUseCase update,
        SetPlatformLegalFileUseCase file,
        PublishPlatformLegalDocumentUseCase publish,
        DeletePlatformLegalDraftUseCase delete,
        PlatformUseCaseScope scope,
        ILogger<LegalDocumentPresenter> logger)
        : base(scope, LegalDocumentState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _update = update ?? throw new ArgumentNullException(nameof(update));
        _file = file ?? throw new ArgumentNullException(nameof(file));
        _publish = publish ?? throw new ArgumentNullException(nameof(publish));
        _delete = delete ?? throw new ArgumentNullException(nameof(delete));
    }

    public async Task LoadAsync(Guid documentId)
    {
        GetPlatformLegalDocumentResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformLegalDocumentRequest { DocumentId = documentId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Detail = response.Detail, Loaded = true, Version = State.Version + 1 }
            : State with { Loaded = true, Error = error });
    }

    public Task SaveAsync(Guid documentId, string? version, bool acceptanceRequired, DateTime? effectiveOn, IReadOnlyList<Guid> parts)
    {
        DateTimeOffset? effective = effectiveOn is { } day ? new DateTimeOffset(DateTime.SpecifyKind(day.Date, DateTimeKind.Utc)) : null;
        return FinishAsync(documentId, RunUseCaseAsync(async (context, cancellationToken) => (await _update.ExecuteAsync(context, new UpdatePlatformLegalDraftRequest
        {
            DocumentId = documentId, Version = version, AcceptanceRequired = acceptanceRequired, EffectiveUtc = effective, IncludedDocumentIds = parts
        }, cancellationToken)).Error), "ui.legal.draft_saved");
    }

    public Task SetFileAsync(Guid documentId, string language, byte[] content, string? summary)
    {
        return FinishAsync(documentId, RunUseCaseAsync(async (context, cancellationToken) => (await _file.ExecuteAsync(context,
            new SetPlatformLegalFileRequest { DocumentId = documentId, Language = language, Content = content, Summary = summary }, cancellationToken)).Error), "ui.legal.file_saved");
    }

    public async Task PublishAsync(Guid documentId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _publish.ExecuteAsync(context, new PublishPlatformLegalDocumentRequest { DocumentId = documentId }, cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null, DoneKey = null });
            return;
        }

        await FinishAsync(documentId, Task.FromResult(error), "ui.legal.published_done");
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public async Task DeleteAsync(Guid documentId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _delete.ExecuteAsync(context, new DeletePlatformLegalDraftRequest { DocumentId = documentId }, cancellationToken)).Error);
        SetState(State with { Deleted = error is null, Error = error });
    }

    public void Fail(Error error)
    {
        SetState(State with { Error = error, DoneKey = null });
    }

    private async Task FinishAsync(Guid documentId, Task<Error?> action, string doneKey)
    {
        Error? error = await action;

        if (error is null)
        {
            await LoadAsync(documentId);
        }

        SetState(State with { StepUpPending = false, Error = error, DoneKey = error is null ? doneKey : null });
    }
}
