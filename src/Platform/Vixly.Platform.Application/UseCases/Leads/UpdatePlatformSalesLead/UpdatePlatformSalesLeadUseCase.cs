using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Sales;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Leads.UpdatePlatformSalesLead;

// =========================================================
// FILE: UpdatePlatformSalesLeadUseCase.cs
//
// PURPOSE:
// Sales staff mark a contact request as contacted or closed, with a note (Stage 2).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs platform.leads.manage
// - The note is internal; the visitor is never told
// =========================================================
public sealed class UpdatePlatformSalesLeadUseCase
{
    private readonly PlatformLeadAdministration _leads;

    public UpdatePlatformSalesLeadUseCase(
        PlatformLeadAdministration leads)
    {
        _leads = leads ?? throw new ArgumentNullException(nameof(leads));
    }

    public async Task<UpdatePlatformSalesLeadResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformSalesLeadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _leads.AuthorizeAsync(context, PlatformPermissions.LeadsManage, cancellationToken) is { } refused)
        {
            return UpdatePlatformSalesLeadResponse.Failure(refused);
        }

        if (await _leads.Store.GetAsync(request.LeadId, cancellationToken) is not PlatformSalesLead lead)
        {
            return UpdatePlatformSalesLeadResponse.Failure(PlatformSalesLeadErrors.LeadNotFound);
        }

        PlatformSalesLeadStatus before = lead.Status;

        try
        {
            if (!lead.ChangeStatus(request.Status, request.Note, context.PlatformUserId, _leads.UtcNow))
            {
                return UpdatePlatformSalesLeadResponse.Success();
            }
        }
        catch (DomainException exception)
        {
            return UpdatePlatformSalesLeadResponse.Failure(exception.Error);
        }

        _leads.Audit.Record(context.PlatformUserId, "platform.leads.status_changed", PlatformPermissions.LeadsManage, nameof(PlatformSalesLead), lead.Id,
            before: new { status = before.ToString() }, after: new { status = lead.Status.ToString() });

        return await _leads.SaveAsync(cancellationToken)
            ? UpdatePlatformSalesLeadResponse.Success()
            : UpdatePlatformSalesLeadResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
