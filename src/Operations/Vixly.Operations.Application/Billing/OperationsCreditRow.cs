using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsCreditRow.cs
//
// PURPOSE:
// A credit account with its customer and where it stands (Slice 5F step 2b, K-6).
//
// RESPONSIBILITIES:
// - Carry what the credit department's list shows
//
// IMPORTANT NOTES:
// - Read only
// =========================================================
public sealed record OperationsCreditRow(OperationsCreditAccount Account, OperationsCustomer? Customer, OperationsCreditPosition Position);
