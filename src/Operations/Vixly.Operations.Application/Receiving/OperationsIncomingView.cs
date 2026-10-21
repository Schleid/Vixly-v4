namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsIncomingView.cs
//
// PURPOSE:
// The views of the incoming packages list (Slice 4D, A-7).
//
// RESPONSIBILITIES:
// - Expected, past their expected day, received, all
//
// IMPORTANT NOTES:
// - "Received" means received by the warehouse
// =========================================================
public enum OperationsIncomingView
{
    All = 0,
    Expected = 1,
    Overdue = 2,
    Received = 3
}
