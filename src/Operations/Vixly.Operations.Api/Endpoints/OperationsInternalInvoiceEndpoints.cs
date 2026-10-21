using Vixly.Contracts.Operations.Billing;
using Vixly.Contracts.Operations.Internal;
using Vixly.Operations.Api.Internal;
using Vixly.Operations.Application.UseCases.Billing.GetMyOperationsInvoiceDocument;
using Vixly.Operations.Application.UseCases.Billing.ListMyOperationsInvoices;
using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Api.Endpoints;

// =========================================================
// FILE: OperationsInternalInvoiceEndpoints.cs
//
// PURPOSE:
// The customer portal's invoice calls (Slice 5F, I-8).
//
// RESPONSIBILITIES:
// - List the customer's invoices with their approved credit notes
// - Send the PDF of an invoice or credit note
//
// IMPORTANT NOTES:
// - Customer scope only; every read checks ownership (S-3)
// =========================================================
public static class OperationsInternalInvoiceEndpoints
{
    public static IEndpointRouteBuilder MapOperationsInternalInvoiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder invoices = endpoints.MapGroup("/internal/v1/customers/me/invoices")
            .WithMetadata(new OperationsInternalEndpointMetadata(OperationsCallerClaims.CustomerScope, true));

        invoices.MapGet("", async (HttpContext http, ListMyOperationsInvoicesUseCase useCase, CancellationToken cancellationToken) =>
        {
            ListMyOperationsInvoicesResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new ListMyOperationsInvoicesRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http) }, cancellationToken);

            return response.Succeeded
                ? Results.Ok(new CustomerInvoicesDto(response.Invoices.Select(invoice => new CustomerInvoiceDto(invoice.Id, invoice.Number,
                    invoice.Kind == OperationsInvoiceKind.Shipment ? "shipment" : "order", invoice.Reference, invoice.IssuedOn, invoice.CurrencyCode, invoice.Total, invoice.CreditedTotal,
                    invoice.Owed, invoice.Status == OperationsInvoiceStatus.Voided,
                    response.CreditNotes.Where(note => note.InvoiceId == invoice.Id && note.Number is not null)
                        .Select(note => new CustomerCreditNoteDto(note.Id, note.Number!, note.DecidedOn ?? invoice.IssuedOn, note.Total)).ToList()))
                    .ToList()))
                : OperationsApiErrors.Problem(http, response.Error!);
        });

        invoices.MapGet("/{invoiceId:guid}/pdf", async (HttpContext http, Guid invoiceId, Guid? credit, GetMyOperationsInvoiceDocumentUseCase useCase, CancellationToken cancellationToken) =>
        {
            GetMyOperationsInvoiceDocumentResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http), new GetMyOperationsInvoiceDocumentRequest
            {
                SessionToken = OperationsInternalPackageEndpoints.Session(http), InvoiceId = invoiceId, CreditNoteId = credit
            }, cancellationToken);

            if (!response.Succeeded)
            {
                return OperationsApiErrors.Problem(http, response.Error!);
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(response.Content, "application/pdf", response.FileName);
        });

        return endpoints;
    }
}
