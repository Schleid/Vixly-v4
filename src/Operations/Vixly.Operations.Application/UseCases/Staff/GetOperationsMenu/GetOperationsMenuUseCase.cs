using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Application.UseCases.Staff.GetOperationsMenu;

// =========================================================
// FILE: GetOperationsMenuUseCase.cs
//
// PURPOSE:
// What the signed-in person may do, so the staff site shows only that (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Permissions held in any scope count: the page itself then shows only the branches and warehouses of that scope
// =========================================================
public sealed class GetOperationsMenuUseCase
{
    private readonly OperationsOwnerCheck _owner;

    public GetOperationsMenuUseCase(OperationsOwnerCheck owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public async Task<GetOperationsMenuResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsMenuRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsMenuResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        return GetOperationsMenuResponse.Success(OperationsPermissions.All.Where(access.Has).ToHashSet(StringComparer.Ordinal), access.IsOwner, access.IsAdministrator);
    }
}
