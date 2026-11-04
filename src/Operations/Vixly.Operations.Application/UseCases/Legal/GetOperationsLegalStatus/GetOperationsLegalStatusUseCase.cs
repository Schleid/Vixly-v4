using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Legal;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Legal.GetOperationsLegalStatus;

// =========================================================
// FILE: GetOperationsLegalStatusUseCase.cs
//
// PURPOSE:
// Reads the tenant's Terms status and the version to accept (Slice 1C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Any signed-in staff may read; only the Owner accepts (Platform D-Lg5)
// =========================================================
public sealed class GetOperationsLegalStatusUseCase
{
    private readonly IOperationsUserRepository _users;

    private readonly OperationsActors _actors;

    private readonly IOperationsLegalClient _legal;

    public GetOperationsLegalStatusUseCase(
        IOperationsUserRepository users,
        OperationsActors actors,
        IOperationsLegalClient legal)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<GetOperationsLegalStatusResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsLegalStatusRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsUser? user = context.UserId is { } userId ? await _users.GetAsync(userId, cancellationToken) : null;

        if (user is null)
        {
            return GetOperationsLegalStatusResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        return GetOperationsLegalStatusResponse.Success(await _legal.GetStatusAsync(_actors.For(context, user), request.Language, cancellationToken));
    }
}
