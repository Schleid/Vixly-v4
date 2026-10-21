namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsIncomingCounts.cs
//
// PURPOSE:
// The counts beside the views of the incoming packages list (Slice 4D, A-7).
//
// RESPONSIBILITIES:
// - Expected, past their expected day, received
//
// IMPORTANT NOTES:
// - Of the warehouses shown, whatever the other filters
// =========================================================
public sealed record OperationsIncomingCounts(int Expected, int Overdue, int Received);
