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

namespace Vixly.Operations.Application.UseCases.Support.GetOperationsSupportCase;

// =========================================================
// FILE: GetOperationsSupportCaseUseCase.cs
//
// PURPOSE:
// Step 2 of a support call: one shipment or package by its tracking number, only when it is the caller's (owner, 2026-10-29, design 9.1, 9.2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Without a caller: the status only, by Vixly's number; the caller id comes protected from the web layer
// =========================================================
public sealed class GetOperationsSupportCaseUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsSupportDesk _desk;

    public GetOperationsSupportCaseUseCase(OperationsAccess access, OperationsSupportDesk desk)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
    }

    public async Task<GetOperationsSupportCaseResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsSupportCaseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsSupportCaseResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!access.HasTenantWide(OperationsPermissions.SupportLookup))
        {
            return GetOperationsSupportCaseResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        (OperationsSupportCase? found, Error? error) = await _desk.CaseAsync(access, request.CallerId, request.Number, cancellationToken);
        return error is null && found is not null ? GetOperationsSupportCaseResponse.Success(found) : GetOperationsSupportCaseResponse.Failure(error ?? OperationsSupportErrors.NotFound);
    }
}
