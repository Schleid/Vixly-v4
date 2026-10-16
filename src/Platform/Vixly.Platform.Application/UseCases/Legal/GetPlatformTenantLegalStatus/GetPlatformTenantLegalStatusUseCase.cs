using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformTenantLegalStatus;

// =========================================================
// FILE: GetPlatformTenantLegalStatusUseCase.cs
//
// PURPOSE:
// Tells Operations where its tenant stands with the Terms and what to show the owner (D-Lg11).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel (Slice 9) for the tenant in the caller context only (S-13)
// =========================================================
public sealed class GetPlatformTenantLegalStatusUseCase
{
    private readonly PlatformLegalAdministration _legal;

    public GetPlatformTenantLegalStatusUseCase(
        PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<GetPlatformTenantLegalStatusResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        GetPlatformTenantLegalStatusRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenantLegalState state = await _legal.Legal.StateAsync(context.PlatformTenantId, cancellationToken);
        PlatformLegalTerms? terms = state.Status is PlatformLegalStatus.Due or PlatformLegalStatus.Overdue ? await _legal.TermsAsync(request.Language, cancellationToken) : null;
        return GetPlatformTenantLegalStatusResponse.Success(new PlatformLegalStatusAnswer(state.Status, state.DeadlineUtc, state.Overdue, terms));
    }
}
