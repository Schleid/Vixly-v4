using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Audit;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Audit.ListOperationsAuditEntries;

// =========================================================
// FILE: ListOperationsAuditEntriesUseCase.cs
//
// PURPOSE:
// Lists the tenant's audit entries for the audit log screen (Slice 1E).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListOperationsAuditEntriesUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsAuditReader _reader;

    private readonly IOperationsUserRepository _users;

    public ListOperationsAuditEntriesUseCase(
        OperationsAccess access,
        IOperationsAuditReader reader,
        IOperationsUserRepository users)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public async Task<ListOperationsAuditEntriesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsAuditEntriesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.AuditView, false, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsAuditEntriesResponse.Failure(refused);
        }

        (IReadOnlyList<OperationsAuditEntry> items, int total) = await _reader.ListAsync(
            new OperationsAuditFilter(request.ActorUserId, request.Area, request.FromUtc, request.ToUtc), Math.Max(request.Page, 1), PageSize, cancellationToken);

        Dictionary<Guid, string> names = new();

        foreach (Guid actor in items.Where(item => item.ActorUserId is not null).Select(item => item.ActorUserId!.Value).Distinct())
        {
            if (await _users.GetAsync(actor, cancellationToken) is { } user)
            {
                names[actor] = user.FullName;
            }
        }

        return ListOperationsAuditEntriesResponse.Success(items
            .Select(item => new OperationsAuditLine(item.Id, item.CreatedUtc, item.ActorUserId, item.ActorUserId is { } id && names.TryGetValue(id, out string? name) ? name : null,
                item.ActionCode, item.TargetType, item.TargetId, item.Reason, item.IpAddress, item.AfterJson ?? item.ArgumentsJson))
            .ToList(), total);
    }

    public const int PageSize = 50;
}
