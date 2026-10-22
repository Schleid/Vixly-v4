using Vixly.Contracts.Operations.Billing;
using Vixly.Contracts.Operations.Internal;
using Vixly.Operations.Api.Internal;
using Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCardPayment;
using Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCreditAccount;
using Vixly.Operations.Application.UseCases.Billing.GetMyOperationsCreditStatement;
using Vixly.Operations.Application.UseCases.Billing.GetMyOperationsInvoiceDocument;
using Vixly.Operations.Application.UseCases.Billing.ListMyOperationsInvoices;
using Vixly.Operations.Application.UseCases.Billing.PayMyOperationsInvoiceByCard;
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
// - Slice 5F step 2b: the customer's credit account and its statement (K-8)
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
                    invoice.Kind switch { OperationsInvoiceKind.Shipment => "shipment", OperationsInvoiceKind.Destination => "destination", _ => "order" }, invoice.Reference, invoice.IssuedOn, invoice.CurrencyCode, invoice.Total, invoice.CreditedTotal,
                    invoice.Owed, invoice.Status == OperationsInvoiceStatus.Voided,
                    response.CreditNotes.Where(note => note.InvoiceId == invoice.Id && note.Number is not null)
                        .Select(note => new CustomerCreditNoteDto(note.Id, note.Number!, note.DecidedOn ?? invoice.IssuedOn, note.Total)).ToList(),
                    invoice.PaymentState switch
                    {
                        OperationsPaymentState.Paid => "paid",
                        OperationsPaymentState.AwaitingConfirmation => "awaiting",
                        OperationsPaymentState.Voided => "voided",
                        OperationsPaymentState.OnAccount => "on_account",
                        OperationsPaymentState.AwaitingCredit => "awaiting_credit",
                        _ => "unpaid"
                    }))
                    .ToList(), response.CardOffered))
                : OperationsApiErrors.Problem(http, response.Error!);
        });

        // Slice 5F step 4: pay an invoice by card on Stripe's page, then follow it.
        invoices.MapPost("/{invoiceId:guid}/card", async (HttpContext http, Guid invoiceId, CustomerCardStartBody body, PayMyOperationsInvoiceByCardUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            PayMyOperationsInvoiceByCardResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http), new PayMyOperationsInvoiceByCardRequest
            {
                SessionToken = OperationsInternalPackageEndpoints.Session(http), InvoiceId = invoiceId, ReturnUrlPrefix = body?.ReturnUrlPrefix ?? string.Empty
            }, cancellationToken);
            return response.Succeeded ? Results.Ok(new CustomerCardStartDto(response.ChargeId)) : OperationsApiErrors.Problem(http, response.Error!);
        });

        invoices.MapGet("/card/{chargeId:guid}", async (HttpContext http, Guid chargeId, GetMyOperationsCardPaymentUseCase useCase, CancellationToken cancellationToken) =>
        {
            GetMyOperationsCardPaymentResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new GetMyOperationsCardPaymentRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http), ChargeId = chargeId }, cancellationToken);
            return response.Succeeded
                ? Results.Ok(new CustomerCardPaymentDto(response.InvoiceId, response.InvoiceNumber, response.Status switch
                {
                    OperationsCardChargeStatus.Paid => "paid",
                    OperationsCardChargeStatus.PaidNotApplied => "paid_not_applied",
                    OperationsCardChargeStatus.Cancelled => "cancelled",
                    _ => "started"
                }, response.Amount, response.CurrencyCode, response.PageUrl))
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

        // Slice 5F step 2b (K-8): the customer's own credit account and its statement.
        RouteGroupBuilder credit = endpoints.MapGroup("/internal/v1/customers/me/credit")
            .WithMetadata(new OperationsInternalEndpointMetadata(OperationsCallerClaims.CustomerScope, true));

        credit.MapGet("", async (HttpContext http, GetMyOperationsCreditAccountUseCase useCase, CancellationToken cancellationToken) =>
        {
            GetMyOperationsCreditAccountResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new GetMyOperationsCreditAccountRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http) }, cancellationToken);

            if (!response.Succeeded)
            {
                return OperationsApiErrors.Problem(http, response.Error!);
            }

            DateOnly today = response.Today;
            return Results.Ok(response.Account is not { } account
                ? new CustomerCreditAccountDto(false, "none", 0m, response.TermsDays, response.CurrencyCode, 0m, 0m, 0m, Array.Empty<CustomerCreditItemDto>())
                : new CustomerCreditAccountDto(true, account.Status.ToString().ToLowerInvariant(), account.Limit, response.TermsDays, response.CurrencyCode, response.Position.Owed,
                    account.IsActive ? response.Position.Available : 0m, response.Position.Overdue,
                    response.Items.Select(item => new CustomerCreditItemDto(item.Id, item.Number, item.Reference, item.IssuedOn, item.DueOn, item.Owed, item.DueOn is { } due && due < today))
                        .ToList()));
        });

        credit.MapGet("/statement", async (HttpContext http, GetMyOperationsCreditStatementUseCase useCase, CancellationToken cancellationToken) =>
        {
            GetMyOperationsCreditStatementResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new GetMyOperationsCreditStatementRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http) }, cancellationToken);

            if (!response.Succeeded)
            {
                return OperationsApiErrors.Problem(http, response.Error!);
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(response.Pdf, "application/pdf", "statement.pdf");
        });

        return endpoints;
    }
}
