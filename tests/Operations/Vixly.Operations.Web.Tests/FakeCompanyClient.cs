using Vixly.Operations.Application.Company;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: FakeCompanyClient.cs
//
// PURPOSE:
// Stands in for Platform's company and billing endpoints in the Operations.Web tests (Slice 1E).
//
// RESPONSIBILITIES:
// - Answer with one company, one open invoice and one PDF
// - Refuse like Platform: owner or admin, and step-up for changes
//
// IMPORTANT NOTES:
// - Tests only
// =========================================================
internal sealed class FakeCompanyClient : IOperationsCompanyClient
{
    public static readonly Guid InvoiceId = Guid.Parse("5d0c1a2b-0000-4000-8000-000000000001");

    public static readonly Guid DocumentId = Guid.Parse("5d0c1a2b-0000-4000-8000-000000000002");

    public List<(OperationsPlatformActor Actor, IReadOnlyList<OperationsCompanyContact> Contacts, string Language, string TimeZone)> Updates { get; } = new();

    public List<(OperationsPlatformActor Actor, OperationsCompanyChangeInput Input)> Changes { get; } = new();

    public OperationsCompanyChange? Change { get; set; }

    public Task<OperationsPlatformCall<OperationsCompany>> GetAsync(OperationsPlatformActor actor, CancellationToken cancellationToken) =>
        Task.FromResult(OperationsPlatformCall<OperationsCompany>.Ok(new OperationsCompany(
            "ACME01",
            new OperationsCompanyDetails("Acme Freight SA", "Acme Freight", "billing@acme.example", "+50922223333", new OperationsCompanyAddress("12 Rue Capois", null, "Port-au-Prince", null, null, "HT")),
            "en",
            "America/Port-au-Prince",
            new[] { new OperationsCompanyContact("owner", "Grace Hopper", "grace@acme.example", null) })));

    public Task<OperationsPlatformCall<bool>> UpdateAsync(OperationsPlatformActor actor, IReadOnlyList<OperationsCompanyContact> contacts, string language, string timeZone, CancellationToken cancellationToken)
    {
        if (Refuse(actor, true) is { } error)
        {
            return Task.FromResult(OperationsPlatformCall<bool>.Refused(error));
        }

        Updates.Add((actor, contacts, language, timeZone));
        return Task.FromResult(OperationsPlatformCall<bool>.Ok(true));
    }

    public Task<OperationsCompanyChange?> GetChangeAsync(OperationsPlatformActor actor, CancellationToken cancellationToken) => Task.FromResult(Change);

    public Task<OperationsPlatformCall<bool>> SubmitChangeAsync(OperationsPlatformActor actor, OperationsCompanyChangeInput input, CancellationToken cancellationToken)
    {
        if (actor.Role != "owner" || !actor.StepUp)
        {
            return Task.FromResult(OperationsPlatformCall<bool>.Refused(actor.Role != "owner" ? OperationsAccessErrors.PermissionRequired : OperationsAccessErrors.StepUpRequired));
        }

        if (input.ProofContent is not { Length: > 0 })
        {
            return Task.FromResult(OperationsPlatformCall<bool>.Refused(OperationsCompanyErrors.ProofRequired));
        }

        Changes.Add((actor, input));
        Change = new OperationsCompanyChange(Guid.NewGuid(), "pending", new OperationsCompanyDetails(input.LegalName, input.TradingName, input.Email, input.Phone,
            new OperationsCompanyAddress(input.AddressLine1, input.AddressLine2, input.City, input.StateProvince, input.PostalCode, input.CountryCode)), input.Reason, actor.Name, DateTimeOffset.UtcNow, null, null);
        return Task.FromResult(OperationsPlatformCall<bool>.Ok(true));
    }

    public Task<OperationsPlatformCall<bool>> WithdrawChangeAsync(OperationsPlatformActor actor, CancellationToken cancellationToken)
    {
        Change = Change is null ? null : Change with { Status = "withdrawn" };
        return Task.FromResult(OperationsPlatformCall<bool>.Ok(true));
    }

    public Task<OperationsSubscription?> GetSubscriptionAsync(OperationsPlatformActor actor, string language, CancellationToken cancellationToken) =>
        Task.FromResult<OperationsSubscription?>(new OperationsSubscription("Growth", "past_due", "monthly", 10, 4, 149m, "USD", null, DateTimeOffset.UtcNow.AddDays(12), false));

    public Task<(IReadOnlyList<OperationsInvoice> Items, int TotalCount)> ListInvoicesAsync(OperationsPlatformActor actor, int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult<(IReadOnlyList<OperationsInvoice>, int)>((new[] { new OperationsInvoice(InvoiceId, "VX-2026-0042", "issued", "USD", 149m, 149m, DateTimeOffset.UtcNow.AddDays(-20), DateTimeOffset.UtcNow.AddDays(-5), true, false) }, 1));

    public Task<(IReadOnlyList<OperationsBillingDocument> Items, int TotalCount)> ListDocumentsAsync(OperationsPlatformActor actor, int page, int pageSize, CancellationToken cancellationToken) =>
        Task.FromResult<(IReadOnlyList<OperationsBillingDocument>, int)>((new[] { new OperationsBillingDocument("invoice", DocumentId, "VX-2026-0042", "VX-2026-0042", "en", DateTimeOffset.UtcNow.AddDays(-20)) }, 1));

    public Task<OperationsLegalFile?> GetDocumentAsync(OperationsPlatformActor actor, string kind, Guid documentId, CancellationToken cancellationToken) =>
        Task.FromResult(kind == "invoice" && documentId == DocumentId ? new OperationsLegalFile("%PDF-1.7 invoice"u8.ToArray(), "application/pdf") : null);

    public Task<OperationsPlatformCall<string>> CreatePayLinkAsync(OperationsPlatformActor actor, Guid invoiceId, CancellationToken cancellationToken) =>
        Task.FromResult(invoiceId == InvoiceId
            ? OperationsPlatformCall<string>.Ok("https://pay.example/link/" + invoiceId.ToString("N"))
            : OperationsPlatformCall<string>.Refused(OperationsCompanyErrors.InvoiceNotPayable));

    private static Vixly.Shared.Kernel.Errors.Error? Refuse(OperationsPlatformActor actor, bool stepUp) =>
        actor.Role is not ("owner" or "admin") ? OperationsAccessErrors.PermissionRequired : stepUp && !actor.StepUp ? OperationsAccessErrors.StepUpRequired : null;
}
