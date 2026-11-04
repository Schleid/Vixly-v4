using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.WithdrawTenantCompanyChange;

// =========================================================
// FILE: WithdrawTenantCompanyChangeUseCase.cs
//
// PURPOSE:
// Lets the tenant owner withdraw its pending company change request (B12).
//
// RESPONSIBILITIES:
// - Check who may act first, then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Owner only; no step-up needed to withdraw
// =========================================================
public sealed class WithdrawTenantCompanyChangeUseCase
{
    private readonly PlatformTenantChanges _changes;

    public WithdrawTenantCompanyChangeUseCase(
        PlatformTenantChanges changes)
    {
        _changes = changes ?? throw new ArgumentNullException(nameof(changes));
    }

    public async Task<WithdrawTenantCompanyChangeResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        WithdrawTenantCompanyChangeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? notAllowed = PlatformTenantAccess.Require(context, false, PlatformTenantActorRole.Owner);

        if (notAllowed is not null)
        {
            return WithdrawTenantCompanyChangeResponse.Failure(notAllowed);
        }

        PlatformTenantChangeRequest? change = await _changes.Requests.GetPendingAsync(context.PlatformTenantId, cancellationToken);

        if (change is null)
        {
            return WithdrawTenantCompanyChangeResponse.Failure(PlatformTenantChangeErrors.NotFound);
        }

        change.Withdraw(_changes.UtcNow);
        _changes.Record(null, "platform.tenants.company_change_withdrawn", null, change,
            arguments: new { actor = context.ActorReference, actorName = context.ActorName });

        return await _changes.SaveAsync(cancellationToken)
            ? WithdrawTenantCompanyChangeResponse.Success()
            : WithdrawTenantCompanyChangeResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
