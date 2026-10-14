using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Tenants.AddPlatformTenantNote;
using Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantNotes;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantNotesPresenter.cs
//
// PURPOSE:
// Behaviour of the notes section (D-T16).
//
// RESPONSIBILITIES:
// - Page through notes, newest first
// - Add a note and show the first page again
//
// IMPORTANT NOTES:
// - Notes are append-only
// =========================================================
public sealed class TenantNotesPresenter : PlatformPresenter<TenantNotesState>
{
    private readonly ListPlatformTenantNotesUseCase _list;

    private readonly AddPlatformTenantNoteUseCase _add;

    public TenantNotesPresenter(
        ListPlatformTenantNotesUseCase list,
        AddPlatformTenantNoteUseCase add,
        PlatformUseCaseScope scope,
        ILogger<TenantNotesPresenter> logger)
        : base(scope, TenantNotesState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _add = add ?? throw new ArgumentNullException(nameof(add));
    }

    public async Task LoadAsync(Guid platformTenantId, int page)
    {
        ListPlatformTenantNotesResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformTenantNotesRequest { PlatformTenantId = platformTenantId, Page = page, PageSize = 10 }, cancellationToken);

            return response.Error;
        });

        SetState(error is null && response?.Notes is { } notes
            ? State with { Notes = notes.Items, Page = notes.Page, TotalPages = notes.TotalPages, Loaded = true, Error = null }
            : State with { Loaded = true, Error = error });
    }

    public async Task<bool> AddAsync(Guid platformTenantId, string? body)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _add.ExecuteAsync(
            context,
            new AddPlatformTenantNoteRequest { PlatformTenantId = platformTenantId, Body = body ?? string.Empty },
            cancellationToken)).Error);

        if (error is not null)
        {
            SetState(State with { DoneKey = null, Error = error });
            return false;
        }

        SetState(State with { DoneKey = "ui.tenants.note_added", Error = null });
        await LoadAsync(platformTenantId, 1);

        return true;
    }
}
