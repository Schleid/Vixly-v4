using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Staff.GetOperationsStaffMember;

// =========================================================
// FILE: GetOperationsStaffMemberUseCase.cs
//
// PURPOSE:
// Reads one staff member with their roles (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetOperationsStaffMemberUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsRoleRepository _roles;

    private readonly OperationsStaffDirectory _directory;

    public GetOperationsStaffMemberUseCase(
        OperationsAccess access,
        IOperationsUserRepository users,
        IOperationsRoleRepository roles,
        OperationsStaffDirectory directory)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
    }

    public async Task<GetOperationsStaffMemberResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsStaffMemberRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.StaffView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsStaffMemberResponse.Failure(refused);
        }

        OperationsUser? user = await _users.GetAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return GetOperationsStaffMemberResponse.Failure(OperationsCommonErrors.NotFound);
        }

        IReadOnlyList<OperationsStaffMember> members = await _directory.DescribeAsync(new[] { user }, await _roles.ListGrantsAsync(user.Id, cancellationToken), cancellationToken);
        return GetOperationsStaffMemberResponse.Success(members[0]);
    }
}
