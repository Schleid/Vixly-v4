using Vixly.Operations.Application.Company;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.GetOperationsBilling;

// =========================================================
// FILE: GetOperationsBillingUseCase.cs
//
// PURPOSE:
// Reads the plan, invoices and billing PDFs for the billing page (Slice 1E).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetOperationsBillingUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsActors _actors;

    private readonly IOperationsCompanyClient _company;

    public GetOperationsBillingUseCase(
        OperationsAccess access,
        OperationsActors actors,
        IOperationsCompanyClient company)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _company = company ?? throw new ArgumentNullException(nameof(company));
    }

    public async Task<GetOperationsBillingResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsBillingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsAccessView? access = await _access.LoadAsync(context, cancellationToken);

        if (access is null)
        {
            return GetOperationsBillingResponse.Failure(Vixly.Operations.Application.Security.OperationsAccessErrors.SignInRequired);
        }

        // The billing page belongs to the Owner and Administrators (design section 3).
        if (!access.IsOwner && !access.IsAdministrator)
        {
            return GetOperationsBillingResponse.Failure(OperationsCompanyErrors.OwnerOrAdministrator);
        }

        OperationsPlatformActor actor = _actors.For(context, access.User);
        OperationsSubscription? subscription = await _company.GetSubscriptionAsync(actor, request.Language, cancellationToken);
        (IReadOnlyList<OperationsInvoice> invoices, int invoiceCount) = await _company.ListInvoicesAsync(actor, Math.Max(request.Page, 1), PageSize, cancellationToken);
        (IReadOnlyList<OperationsBillingDocument> documents, int documentCount) = await _company.ListDocumentsAsync(actor, 1, PageSize, cancellationToken);

        return GetOperationsBillingResponse.Success(new OperationsBillingView(subscription, invoices, invoiceCount, documents, documentCount));
    }

    public const int PageSize = 20;
}
