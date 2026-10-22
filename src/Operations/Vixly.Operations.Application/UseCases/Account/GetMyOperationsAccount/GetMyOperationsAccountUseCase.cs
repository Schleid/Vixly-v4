using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Account.GetMyOperationsAccount;

// =========================================================
// FILE: GetMyOperationsAccountUseCase.cs
//
// PURPOSE:
// Reads my account: second factor, recovery codes left and open sessions (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetMyOperationsAccountUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsRoleRepository _roles;

    private readonly IOperationsSessionRepository _sessions;

    private readonly OperationsStaffDirectory _directory;

    public GetMyOperationsAccountUseCase(
        OperationsAccess access,
        IOperationsRoleRepository roles,
        IOperationsSessionRepository sessions,
        OperationsStaffDirectory directory)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
    }

    public async Task<GetMyOperationsAccountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetMyOperationsAccountResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsStaffMember> me = await _directory.DescribeAsync(new[] { access.User }, await _roles.ListGrantsAsync(access.User.Id, cancellationToken), cancellationToken);
        IReadOnlyList<OperationsSessionSummary> sessions = (await _sessions.ListOpenAsync(access.User.Id, cancellationToken))
            .OrderByDescending(session => session.LastSeenUtc)
            .Select(session => new OperationsSessionSummary(session.Id, session.CreatedUtc, session.LastSeenUtc, session.IpAddress, session.UserAgent, session.Id == context.SessionId))
            .ToList();

        return GetMyOperationsAccountResponse.Success(me[0], access.User.UnusedRecoveryCodeCount, sessions, access.User.HasSecondFactor && await _access.IsFreshAsync(context, cancellationToken));
    }
}
