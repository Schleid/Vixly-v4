using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Users.GetPlatformUserActions;

// =========================================================
// FILE: GetPlatformUserActionsUseCase.cs
//
// PURPOSE:
// Tells the user detail screen which administrator actions to
// offer for one staff user.
//
// RESPONSIBILITIES:
// - Require platform.users.view
// - Read the target facts and the actor's fresh access, then
//   apply PlatformUserAdminActionRules
//
// IMPORTANT NOTES:
// - Read-only: no audit entry
// =========================================================
public sealed class GetPlatformUserActionsUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformUserQueries _queries;

    private readonly IPlatformPermissionReader _permissions;

    private readonly TimeProvider _timeProvider;

    public GetPlatformUserActionsUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformUserQueries queries,
        IPlatformPermissionReader permissions,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetPlatformUserActionsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformUserActionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.UsersView, cancellationToken);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        PlatformUserActionFacts? target = await _queries.GetActionFactsAsync(request.PlatformUserId, utcNow, cancellationToken);

        if (target is null)
        {
            return GetPlatformUserActionsResponse.Failure(PlatformCommonErrors.NotFound);
        }

        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(context.PlatformUserId, utcNow, cancellationToken);

        return GetPlatformUserActionsResponse.Success(actor is null
            ? Array.Empty<PlatformUserAdminAction>()
            : PlatformUserAdminActionRules.Available(target, context.PlatformUserId, actor));
    }
}
