using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsCarriers;

// =========================================================
// FILE: GetOperationsCarriersUseCase.cs
//
// PURPOSE:
// The company's carriers (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.carriers.manage (the company's carriers and their rates, D-8)
// =========================================================
public sealed class GetOperationsCarriersUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsDispatchRepository _dispatches;

    public GetOperationsCarriersUseCase(
        OperationsAccess access,
        IOperationsDispatchRepository dispatches)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
    }

    public async Task<GetOperationsCarriersResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCarriersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CarriersManage, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCarriersResponse.Failure(refused);
        }

        return GetOperationsCarriersResponse.Success(await _dispatches.ListCarriersAsync(cancellationToken));
    }
}
