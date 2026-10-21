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

namespace Vixly.Operations.Application.UseCases.Staff.ListOperationsStaff;

// =========================================================
// FILE: ListOperationsStaffUseCase.cs
//
// PURPOSE:
// Lists the staff of the tenant, by page (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Paged (P-1): 25 per page
// =========================================================
public sealed class ListOperationsStaffUseCase
{
    public const int PageSize = 25;

    private readonly OperationsAccess _access;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsRoleRepository _roles;

    private readonly OperationsStaffDirectory _directory;

    public ListOperationsStaffUseCase(
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

    public async Task<ListOperationsStaffResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsStaffRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.StaffView, false, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsStaffResponse.Failure(refused);
        }

        (IReadOnlyList<OperationsUser> users, int total) = await _users.ListAsync(request.Search, request.Page, PageSize, cancellationToken);
        IReadOnlyList<OperationsStaffMember> members = await _directory.DescribeAsync(users, await _roles.ListActiveGrantsAsync(cancellationToken), cancellationToken);
        return ListOperationsStaffResponse.Success(members, total);
    }
}
