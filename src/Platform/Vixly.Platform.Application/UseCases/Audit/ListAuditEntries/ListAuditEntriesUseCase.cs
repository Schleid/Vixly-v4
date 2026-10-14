using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Audit.ListAuditEntries;

// =========================================================
// FILE: ListAuditEntriesUseCase.cs
//
// PURPOSE:
// Pages through the audit log, newest first.
//
// RESPONSIBILITIES:
// - Require platform.audit.view
// - Accept only known action codes and a window whose start is
//   before its end
// - Clamp the page size
//
// IMPORTANT NOTES:
// - Read-only; viewing the log is not itself audited
// =========================================================
public sealed class ListAuditEntriesUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformAuditQueries _queries;

    public ListAuditEntriesUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformAuditQueries queries)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListAuditEntriesResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListAuditEntriesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.AuditView, cancellationToken);

        string? actionCode = string.IsNullOrWhiteSpace(request.ActionCode) ? null : request.ActionCode;

        if ((actionCode is not null && !PlatformAuditActions.IsKnown(actionCode))
            || (request.FromUtc is { } from && request.ToUtc is { } to && from >= to))
        {
            return ListAuditEntriesResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        PagedResult<PlatformAuditListItem> entries = await _queries.ListAsync(
            new PlatformAuditFilter(request.ActorUserId, request.TargetId, actionCode, request.FromUtc, request.ToUtc),
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken);

        return ListAuditEntriesResponse.Success(entries);
    }
}
