using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsCreditStatement.cs
//
// PURPOSE:
// What a credit account's statement shows (Slice 5F step 2b, K-7).
//
// RESPONSIBILITIES:
// - Carry the company, the customer, the date, the open items and the position
//
// IMPORTANT NOTES:
// - Printed in the customer's language
// =========================================================
public sealed record OperationsCreditStatement(string CompanyName, OperationsCustomer Customer, OperationsCreditAccount Account, DateOnly Day, string CurrencyCode, int TermsDays,
    IReadOnlyList<Domain.Billing.OperationsInvoice> Items, OperationsCreditPosition Position);
