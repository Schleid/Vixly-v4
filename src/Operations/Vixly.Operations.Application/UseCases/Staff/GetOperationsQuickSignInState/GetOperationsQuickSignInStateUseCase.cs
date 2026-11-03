using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.GetOperationsQuickSignInState;

// =========================================================
// FILE: GetOperationsQuickSignInStateUseCase.cs
//
// PURPOSE:
// Whether a person has a badge in use and a PIN (Slice 12A): for the staff page, or the person's own account.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Another person's state needs View staff
// =========================================================
public sealed class GetOperationsQuickSignInStateUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsQuickSignInRepository _quick;

    public GetOperationsQuickSignInStateUseCase(
        OperationsAccess access,
        IOperationsQuickSignInRepository quick)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _quick = quick ?? throw new ArgumentNullException(nameof(quick));
    }

    public async Task<GetOperationsQuickSignInStateResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsQuickSignInStateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Guid userId;

        if (request.UserId is { } other)
        {
            (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.StaffView, false, cancellationToken);

            if (refused is not null)
            {
                return GetOperationsQuickSignInStateResponse.Failure(refused);
            }

            userId = other;
        }
        else if (await _access.LoadAsync(context, cancellationToken) is { } self)
        {
            userId = self.User.Id;
        }
        else
        {
            return GetOperationsQuickSignInStateResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        Vixly.Operations.Domain.Identity.OperationsStaffBadge? badge = await _quick.FindActiveBadgeAsync(userId, cancellationToken);
        Vixly.Operations.Domain.Identity.OperationsStaffPin? pin = await _quick.FindPinAsync(userId, cancellationToken);
        return GetOperationsQuickSignInStateResponse.Success(badge is not null, badge?.CreatedUtc, pin is not null, pin?.IsLocked == true);
    }
}
