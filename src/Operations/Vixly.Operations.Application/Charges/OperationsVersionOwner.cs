namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsVersionOwner.cs
//
// PURPOSE:
// What a scheduled version belongs to (Slice 4B).
//
// RESPONSIBILITIES:
// - Name the three kinds of dated prices
//
// IMPORTANT NOTES:
// - Not stored
// =========================================================
public enum OperationsVersionOwner
{
    PriceList = 1,
    Extra = 2,
    Insurance = 3
}
