using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsParcelMatch.cs
//
// PURPOSE:
// Who a scanned package belongs to (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the matched pre-alert and customer, and why
//
// IMPORTANT NOTES:
// - Outcome: pre_alert, conflict or unidentified
// =========================================================
public sealed record OperationsParcelMatch(OperationsPreAlert? PreAlert, OperationsCustomer? Customer, string Outcome);
