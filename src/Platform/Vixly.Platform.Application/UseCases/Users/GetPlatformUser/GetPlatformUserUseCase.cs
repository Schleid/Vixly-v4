using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Users.GetPlatformUser;

// =========================================================
// FILE: GetPlatformUserUseCase.cs
//
// PURPOSE:
// Reads one staff user for the user detail screen.
//
// RESPONSIBILITIES:
// - Require platform.users.view
// - Answer NotFound for an unknown id
//
// IMPORTANT NOTES:
// - Read-only: no audit entry
// =========================================================
public sealed class GetPlatformUserUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformUserQueries _queries;

    private readonly TimeProvider _timeProvider;

    public GetPlatformUserUseCase(IPlatformAuthorizationGate gate, IPlatformUserQueries queries, TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetPlatformUserResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformUserRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.UsersView, cancellationToken);

        PlatformUserDetail? user = await _queries.GetAsync(request.PlatformUserId, _timeProvider.GetUtcNow(), cancellationToken);

        return user is null
            ? GetPlatformUserResponse.Failure(PlatformCommonErrors.NotFound)
            : GetPlatformUserResponse.Success(user);
    }
}
