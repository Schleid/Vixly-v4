using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Legal.CreatePlatformLegalDraft;
using Vixly.Platform.Application.UseCases.Legal.ListPlatformLegalDocuments;
using Vixly.Platform.Domain.Legal;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Legal;

// =========================================================
// FILE: LegalDocumentsPresenter.cs
//
// PURPOSE:
// Behaviour of the legal documents list (Slice 8).
//
// RESPONSIBILITIES:
// - Load one page of versions, optionally of one kind
// - Start a new draft
//
// IMPORTANT NOTES:
// - The page opens the new draft once it is created
// =========================================================
public sealed class LegalDocumentsPresenter : PlatformPresenter<LegalDocumentsState>
{
    private readonly ListPlatformLegalDocumentsUseCase _list;

    private readonly CreatePlatformLegalDraftUseCase _create;

    public LegalDocumentsPresenter(ListPlatformLegalDocumentsUseCase list, CreatePlatformLegalDraftUseCase create, PlatformUseCaseScope scope, ILogger<LegalDocumentsPresenter> logger)
        : base(scope, LegalDocumentsState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _create = create ?? throw new ArgumentNullException(nameof(create));
    }

    public async Task LoadAsync(PlatformLegalDocumentKind? kind, int page)
    {
        ListPlatformLegalDocumentsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformLegalDocumentsRequest { Kind = kind, Page = page }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Documents is { } result
            ? State with { Items = result.Items, Page = result.Page, TotalPages = result.TotalPages, Abilities = response.Abilities, Loaded = true, Error = null }
            : State with { Loaded = true, Error = error });
    }

    public async Task CreateAsync(PlatformLegalDocumentKind kind, string? version)
    {
        CreatePlatformLegalDraftResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _create.ExecuteAsync(context, new CreatePlatformLegalDraftRequest { Kind = kind, Version = version }, cancellationToken);
            return response.Error;
        });

        SetState(State with { CreatedId = error is null ? response?.DocumentId : null, Error = error });
    }
}
