using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Security;

// =========================================================
// FILE: OperationsOwnerCheck.cs
//
// PURPOSE:
// Lets only the signed-in Owner through (Slice 1B, until roles arrive in 1C).
//
// RESPONSIBILITIES:
// - Load the signed-in user and check it is the active Owner
//
// IMPORTANT NOTES:
// - Slice 1C replaces it with permissions (ops.brand.edit, ops.branches.manage ...)
// =========================================================
public sealed class OperationsOwnerCheck
{
    private readonly IOperationsUserRepository _users;

    private readonly OperationsAccess? _access;

    public OperationsOwnerCheck(IOperationsUserRepository users)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public OperationsOwnerCheck(IOperationsUserRepository users, OperationsAccess access)
        : this(users)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
    }

    // Slice 1D: what the signed-in user may do; without the access service only an Owner gets a view.
    public async Task<OperationsAccessView?> LoadAsync(OperationsAuthorizationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_access is not null)
        {
            return await _access.LoadAsync(context, cancellationToken);
        }

        OperationsUser? user = context.UserId is { } userId ? await _users.GetAsync(userId, cancellationToken) : null;
        return user is { IsOwner: true, Account.IsActive: true } ? new OperationsAccessView(user, Array.Empty<(Vixly.Operations.Domain.Access.OperationsRoleGrant, Vixly.Operations.Domain.Access.OperationsRole)>()) : null;
    }

    // Slice 1D: a permission instead of "Owner only"; the Owner always passes.
    public async Task<Error?> RefuseUnlessAllowedAsync(OperationsAuthorizationContext context, string permission, CancellationToken cancellationToken)
    {
        if (_access is null)
        {
            return await RefuseUnlessOwnerAsync(context, cancellationToken);
        }

        (OperationsAccessView? _, Error? refused) = await _access.RequireAsync(context, permission, false, cancellationToken);
        return refused;
    }

    public async Task<Error?> RefuseUnlessOwnerAsync(OperationsAuthorizationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.UserId is not { } userId)
        {
            return OperationsAccessErrors.SignInRequired;
        }

        OperationsUser? user = await _users.GetAsync(userId, cancellationToken);
        return user is { IsOwner: true, Account.IsActive: true } ? null : OperationsAccessErrors.OwnerOnly;
    }
}
