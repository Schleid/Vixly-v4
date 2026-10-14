using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Audit.ListAuditEntries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Audit;

// =========================================================
// FILE: AuditLogPresenter.cs
//
// PURPOSE:
// Behaviour of the audit log.
//
// RESPONSIBILITIES:
// - Load one page of entries for the filters in the URL
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// =========================================================
public sealed class AuditLogPresenter : PlatformPresenter<AuditLogState>
{
    private readonly ListAuditEntriesUseCase _list;

    public AuditLogPresenter(
        ListAuditEntriesUseCase list,
        PlatformUseCaseScope scope,
        ILogger<AuditLogPresenter> logger)
        : base(scope, AuditLogState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(AuditLogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        ListAuditEntriesResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(
                context,
                new ListAuditEntriesRequest
                {
                    ActorUserId = query.ActorUserId,
                    TargetId = query.TargetId,
                    ActionCode = query.ActionCode,
                    FromUtc = query.FromUtc,
                    ToUtc = query.ToUtc,
                    Page = query.Page
                },
                cancellationToken);

            return response.Error;
        });

        SetState(error is null && response?.Entries is { } entries
            ? new AuditLogState(true, entries.Items, entries.Page, entries.TotalPages, entries.TotalCount, null)
            : AuditLogState.Initial with { Loaded = true, Error = error });
    }
}
