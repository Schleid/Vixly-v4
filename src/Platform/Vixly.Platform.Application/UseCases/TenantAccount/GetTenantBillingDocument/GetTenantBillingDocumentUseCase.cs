using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantBillingDocument;

// =========================================================
// FILE: GetTenantBillingDocumentUseCase.cs
//
// PURPOSE:
// One of the tenant's billing PDFs (call 22).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Any tenant role
// =========================================================
public sealed class GetTenantBillingDocumentUseCase
{
    private readonly PlatformBillingAdministration _billing;

    public GetTenantBillingDocumentUseCase(
        PlatformBillingAdministration billing)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    public async Task<GetTenantBillingDocumentResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        GetTenantBillingDocumentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformBillingDocument? document = Enum.IsDefined(request.Kind)
            ? await _billing.Store.GetDocumentAsync(request.Kind, request.OwnerId, cancellationToken)
            : null;
        PlatformInvoice? invoice = document is null ? null : await _billing.Invoices.GetAsync(document.PlatformInvoiceId, cancellationToken);

        // Another tenant's document looks like a missing one (S-3).
        return document is null || invoice is null || invoice.PlatformTenantId != context.PlatformTenantId
            ? GetTenantBillingDocumentResponse.Failure(PlatformBillingErrors.DocumentNotFound)
            : GetTenantBillingDocumentResponse.Success(document.Content, document.FileName);
    }
}
