using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.UseCases.Leads.GetPlatformSalesLead;

// =========================================================
// FILE: GetPlatformSalesLeadUseCase.cs
//
// PURPOSE:
// One contact request, and whether the reader may update it (Stage 2).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs platform.leads.view
// =========================================================
public sealed class GetPlatformSalesLeadUseCase
{
    private readonly PlatformLeadAdministration _leads;

    private readonly IPlatformPermissionReader _permissions;

    public GetPlatformSalesLeadUseCase(
        PlatformLeadAdministration leads,
        IPlatformPermissionReader permissions)
    {
        _leads = leads ?? throw new ArgumentNullException(nameof(leads));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }

    public async Task<GetPlatformSalesLeadResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformSalesLeadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _leads.AuthorizeAsync(context, PlatformPermissions.LeadsView, cancellationToken) is { } refused)
        {
            return GetPlatformSalesLeadResponse.Failure(refused);
        }

        if (await _leads.Store.GetItemAsync(request.LeadId, cancellationToken) is not PlatformSalesLeadItem lead)
        {
            return GetPlatformSalesLeadResponse.Failure(PlatformSalesLeadErrors.LeadNotFound);
        }

        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(context.PlatformUserId, _leads.UtcNow, cancellationToken);

        return GetPlatformSalesLeadResponse.Success(lead, actor is not null && actor.IsAccountActive && PlatformAccess.Holds(actor, PlatformPermissions.LeadsManage));
    }
}
