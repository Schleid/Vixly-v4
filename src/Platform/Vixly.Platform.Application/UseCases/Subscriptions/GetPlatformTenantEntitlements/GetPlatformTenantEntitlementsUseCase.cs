using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.GetPlatformTenantEntitlements;

// =========================================================
// FILE: GetPlatformTenantEntitlementsUseCase.cs
//
// PURPOSE:
// Reads what a tenant may use: access, features and effective limits.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The same answer Operations will read through Contracts.Platform (Slice 9)
// =========================================================
public sealed class GetPlatformTenantEntitlementsUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformCollectionsQueries _collections;

    private readonly PlatformLegalGate _legal;

    public GetPlatformTenantEntitlementsUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformCollectionsQueries collections,
        PlatformLegalGate legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }

    public async Task<GetPlatformTenantEntitlementsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformTenantEntitlementsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _administration.AuthorizeAsync(context, PlatformPermissions.SubscriptionsView, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return GetPlatformTenantEntitlementsResponse.Failure(refused);
        }

        return GetPlatformTenantEntitlementsResponse.Success(await new PlatformEntitlementsReader(_administration, _collections, _legal).ReadAsync(request.PlatformTenantId, cancellationToken));
    }
}
