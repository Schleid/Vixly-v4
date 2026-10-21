using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.SubmitTenantCompanyChange;

// =========================================================
// FILE: SubmitTenantCompanyChangeUseCase.cs
//
// PURPOSE:
// Lets the tenant owner ask TechCrafters to change the company’s legal details, with proof (B12, D-T8).
//
// RESPONSIBILITIES:
// - Check who may act first, then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Owner only, with a checked second factor (the token’s step-up claim)
// - One pending request per tenant
// - Legal details change only this way once the tenant exists
// =========================================================
public sealed class SubmitTenantCompanyChangeUseCase
{
    private readonly PlatformTenantChanges _changes;

    public SubmitTenantCompanyChangeUseCase(
        PlatformTenantChanges changes)
    {
        _changes = changes ?? throw new ArgumentNullException(nameof(changes));
    }

    public async Task<SubmitTenantCompanyChangeResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        SubmitTenantCompanyChangeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? notAllowed = PlatformTenantAccess.Require(context, true, PlatformTenantActorRole.Owner);

        if (notAllowed is not null)
        {
            return SubmitTenantCompanyChangeResponse.Failure(notAllowed);
        }

        PlatformTenant? tenant = await _changes.Tenants.GetAsync(context.PlatformTenantId, cancellationToken);

        if (tenant is null || tenant.Status is PlatformTenantStatus.Banned or PlatformTenantStatus.Deactivated)
        {
            return SubmitTenantCompanyChangeResponse.Failure(PlatformCommonErrors.NotFound);
        }

        if (await _changes.Requests.GetPendingAsync(tenant.Id, cancellationToken) is not null)
        {
            return SubmitTenantCompanyChangeResponse.Failure(PlatformTenantChangeErrors.AlreadyPending);
        }

        PlatformTenantChangeRequest change;

        try
        {
            change = PlatformTenantChangeRequest.Submit(
                tenant,
                request.Company.ToDetails(),
                request.Reason,
                request.ProofFileName,
                request.ProofContent,
                context.ActorReference,
                context.ActorName,
                _changes.UtcNow);
        }
        catch (DomainException exception)
        {
            return SubmitTenantCompanyChangeResponse.Failure(exception.Error);
        }

        _changes.Requests.Add(change);
        _changes.Record(null, "platform.tenants.company_change_submitted", null, change,
            arguments: new { actor = context.ActorReference, actorName = context.ActorName, role = PlatformTenantAccess.RoleName(context.Role), proof = change.Proof.Sha256 },
            before: PlatformTenantChanges.Describe(change.Previous),
            after: PlatformTenantChanges.Describe(change.Proposed));
        _changes.Notify(Vixly.Platform.Application.Messaging.PlatformMessageTypes.TenantCompanyChangeSubmitted, change);

        // The filtered unique index refuses a second pending request that raced this one.
        return await _changes.SaveAsync(cancellationToken)
            ? SubmitTenantCompanyChangeResponse.Success(change.Id)
            : SubmitTenantCompanyChangeResponse.Failure(PlatformTenantChangeErrors.AlreadyPending);
    }
}
