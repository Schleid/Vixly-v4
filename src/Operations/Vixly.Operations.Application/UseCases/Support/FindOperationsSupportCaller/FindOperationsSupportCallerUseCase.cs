using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Support;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Support;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Support.FindOperationsSupportCaller;

// =========================================================
// FILE: FindOperationsSupportCallerUseCase.cs
//
// PURPOSE:
// Step 1 of a support call: the caller's full phone number names one account, briefly (owner, 2026-10-29, S-7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.support.lookup for the whole company; no account (or two with one number) is a success without a caller
// =========================================================
public sealed class FindOperationsSupportCallerUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsSupportDesk _desk;

    public FindOperationsSupportCallerUseCase(OperationsAccess access, OperationsSupportDesk desk)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
    }

    public async Task<FindOperationsSupportCallerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        FindOperationsSupportCallerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return FindOperationsSupportCallerResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!access.HasTenantWide(OperationsPermissions.SupportLookup))
        {
            return FindOperationsSupportCallerResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        (OperationsSupportCaller? caller, Error? error) = await _desk.CallerAsync(access, request.Phone, cancellationToken);
        return error is null ? FindOperationsSupportCallerResponse.Success(caller) : FindOperationsSupportCallerResponse.Failure(error);
    }
}
