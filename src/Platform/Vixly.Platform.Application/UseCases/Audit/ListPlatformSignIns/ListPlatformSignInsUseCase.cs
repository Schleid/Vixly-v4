using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Audit.ListPlatformSignIns;

// =========================================================
// FILE: ListPlatformSignInsUseCase.cs
//
// PURPOSE:
// Member activity in Control (owner, 2026-10-21): sign-ins and sign-outs, with address, browser, system and country, newest first.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - platform.audit.view: the same people who read the audit log
// =========================================================
public sealed class ListPlatformSignInsUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformUserQueries _queries;

    private readonly TimeProvider _timeProvider;

    public ListPlatformSignInsUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformUserQueries queries,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListPlatformSignInsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformSignInsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.AuditView, cancellationToken);

        return ListPlatformSignInsResponse.Success(await _queries.ListSignInsAsync(request.PlatformUserId, _timeProvider.GetUtcNow(),
            PagedRequest.Create(request.Page, request.PageSize), cancellationToken));
    }
}
