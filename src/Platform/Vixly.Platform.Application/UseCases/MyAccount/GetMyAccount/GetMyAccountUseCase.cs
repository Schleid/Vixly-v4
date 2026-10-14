using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.MyAccount.GetMyAccount;

// =========================================================
// FILE: GetMyAccountUseCase.cs
//
// PURPOSE:
// Reads the signed-in user's own account for My account.
//
// RESPONSIBILITIES:
// - Read the profile, second factors and open sessions of the
//   user in the context, and say which session is this one
//
// IMPORTANT NOTES:
// - No permission needed: everyone may see their own account
// - Read-only: no audit entry
// =========================================================
public sealed class GetMyAccountUseCase
{
    private readonly IPlatformUserQueries _queries;

    private readonly TimeProvider _timeProvider;

    public GetMyAccountUseCase(IPlatformUserQueries queries, TimeProvider timeProvider)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetMyAccountResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetMyAccountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformUserDetail? account = await _queries.GetAsync(context.PlatformUserId, _timeProvider.GetUtcNow(), cancellationToken);

        return account is null
            ? GetMyAccountResponse.Failure(PlatformCommonErrors.NotFound)
            : GetMyAccountResponse.Success(account, context.SessionId);
    }
}
