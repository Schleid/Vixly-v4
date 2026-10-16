using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing.BillPlatformChargesNow;
using Vixly.Platform.Application.UseCases.Billing.GetPlatformTenantBilling;
using Vixly.Platform.Application.UseCases.Billing.SetPlatformTenantTaxDetails;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: TenantBillingPresenter.cs
//
// PURPOSE:
// Behaviour of the Billing section of the tenant page.
//
// RESPONSIBILITIES:
// - Load balances per currency and the tax applied
// - Change the tax ID and exemption (D-B16)
// - Bill the pending one-time charges now (D-B5)
//
// IMPORTANT NOTES:
// - Behaviour only; markup lives in the component (ADR-0001)
// =========================================================
public sealed class TenantBillingPresenter : PlatformPresenter<TenantBillingState>
{
    private readonly GetPlatformTenantBillingUseCase _get;

    private readonly SetPlatformTenantTaxDetailsUseCase _tax;

    private readonly BillPlatformChargesNowUseCase _bill;

    public TenantBillingPresenter(
        GetPlatformTenantBillingUseCase get,
        SetPlatformTenantTaxDetailsUseCase tax,
        BillPlatformChargesNowUseCase bill,
        PlatformUseCaseScope scope,
        ILogger<TenantBillingPresenter> logger)
        : base(scope, TenantBillingState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _tax = tax ?? throw new ArgumentNullException(nameof(tax));
        _bill = bill ?? throw new ArgumentNullException(nameof(bill));
    }

    public async Task LoadAsync(Guid tenantId)
    {
        GetPlatformTenantBillingResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformTenantBillingRequest { PlatformTenantId = tenantId }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Billing = response.Billing, Loaded = true, Version = State.Version + 1, Error = null }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public Task<bool> SetTaxAsync(Guid tenantId, string? taxId, bool exempt, string? reason, string? reference)
    {
        return ActAsync(tenantId, "ui.tenant_billing.tax_saved", async (context, cancellationToken) => (await _tax.ExecuteAsync(
            context,
            new SetPlatformTenantTaxDetailsRequest { PlatformTenantId = tenantId, TaxId = taxId, TaxExempt = exempt, ExemptionReason = reason, ExemptionReference = reference },
            cancellationToken)).Error);
    }

    public Task<bool> BillChargesAsync(Guid tenantId)
    {
        return ActAsync(tenantId, "ui.tenant_billing.charges_billed", async (context, cancellationToken) => (await _bill.ExecuteAsync(
            context, new BillPlatformChargesNowRequest { PlatformTenantId = tenantId }, cancellationToken)).Error);
    }

    private async Task<bool> ActAsync(Guid tenantId, string doneKey, Func<PlatformAuthorizationContext, CancellationToken, Task<Error?>> work)
    {
        Error? error = await RunUseCaseAsync(work);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return false;
        }

        if (error is null)
        {
            await LoadAsync(tenantId);
        }

        SetState(State with { StepUpPending = false, DoneKey = error is null ? doneKey : null, Error = error });

        return error is null;
    }
}
