namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsCompanyClient.cs
//
// PURPOSE:
// The company's account at Vixly: details, contacts, change requests and billing (Platform, Slice 1E).
//
// RESPONSIBILITIES:
// - Read and change what Platform keeps about the company
// - Read the plan, invoices and billing PDFs; ask for a pay link
//
// IMPORTANT NOTES:
// - Platform checks the role (owner, admin) and the step-up of the actor
// =========================================================
public interface IOperationsCompanyClient
{
    Task<OperationsPlatformCall<OperationsCompany>> GetAsync(OperationsPlatformActor actor, CancellationToken cancellationToken);

    Task<OperationsPlatformCall<bool>> UpdateAsync(OperationsPlatformActor actor, IReadOnlyList<OperationsCompanyContact> contacts, string language, string timeZone, CancellationToken cancellationToken);

    Task<OperationsCompanyChange?> GetChangeAsync(OperationsPlatformActor actor, CancellationToken cancellationToken);

    Task<OperationsPlatformCall<bool>> SubmitChangeAsync(OperationsPlatformActor actor, OperationsCompanyChangeInput input, CancellationToken cancellationToken);

    Task<OperationsPlatformCall<bool>> WithdrawChangeAsync(OperationsPlatformActor actor, CancellationToken cancellationToken);

    Task<OperationsSubscription?> GetSubscriptionAsync(OperationsPlatformActor actor, string language, CancellationToken cancellationToken);

    Task<(IReadOnlyList<OperationsInvoice> Items, int TotalCount)> ListInvoicesAsync(OperationsPlatformActor actor, int page, int pageSize, CancellationToken cancellationToken);

    Task<(IReadOnlyList<OperationsBillingDocument> Items, int TotalCount)> ListDocumentsAsync(OperationsPlatformActor actor, int page, int pageSize, CancellationToken cancellationToken);

    Task<OperationsLegalFile?> GetDocumentAsync(OperationsPlatformActor actor, string kind, Guid documentId, CancellationToken cancellationToken);

    Task<OperationsPlatformCall<string>> CreatePayLinkAsync(OperationsPlatformActor actor, Guid invoiceId, CancellationToken cancellationToken);
}
