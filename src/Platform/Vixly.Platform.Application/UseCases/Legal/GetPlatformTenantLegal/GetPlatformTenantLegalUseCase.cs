using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformTenantLegal;

// =========================================================
// FILE: GetPlatformTenantLegalUseCase.cs
//
// PURPOSE:
// Reads the Legal section of a tenant page: status, acceptances, reminders and actions.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformTenantLegalUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public GetPlatformTenantLegalUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<GetPlatformTenantLegalResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformTenantLegalRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _legal.AuthorizeAsync(context, PlatformPermissions.LegalView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformTenantLegalResponse.Failure(refused);
        }

        return GetPlatformTenantLegalResponse.Success(new PlatformTenantLegalDetail(
            await _legal.Legal.StateAsync(request.PlatformTenantId, cancellationToken),
            await _legal.Queries.ListAcceptancesAsync(request.PlatformTenantId, cancellationToken),
            await _legal.Queries.ListRemindersAsync(request.PlatformTenantId, cancellationToken),
            await _legal.AbilitiesAsync(context, cancellationToken)));
    }
}
