using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Leads.GetPlatformSalesLead;
using Vixly.Platform.Application.UseCases.Leads.UpdatePlatformSalesLead;
using Vixly.Platform.Domain.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: LeadPresenter.cs
//
// PURPOSE:
// Behaviour of the lead page.
//
// RESPONSIBILITIES:
// - Load the lead
// - Save its status and internal note
//
// IMPORTANT NOTES:
// - The note is internal; the visitor is never told
// =========================================================
public sealed class LeadPresenter : PlatformPresenter<LeadState>
{
    private readonly GetPlatformSalesLeadUseCase _get;

    private readonly UpdatePlatformSalesLeadUseCase _update;

    public LeadPresenter(GetPlatformSalesLeadUseCase get, UpdatePlatformSalesLeadUseCase update, PlatformUseCaseScope scope, ILogger<LeadPresenter> logger)
        : base(scope, LeadState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _update = update ?? throw new ArgumentNullException(nameof(update));
    }

    public async Task LoadAsync(Guid leadId)
    {
        GetPlatformSalesLeadResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformSalesLeadRequest { LeadId = leadId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Lead = response.Lead, CanManage = response.CanManage, Loaded = true }
            : State with { Loaded = true, Error = error });
    }

    public async Task<bool> SaveAsync(Guid leadId, PlatformSalesLeadStatus status, string? note)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _update.ExecuteAsync(context,
            new UpdatePlatformSalesLeadRequest { LeadId = leadId, Status = status, Note = note }, cancellationToken)).Error);

        await LoadAsync(leadId);
        SetState(State with { Saved = error is null, Error = error });
        return error is null;
    }
}
