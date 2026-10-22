namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsQuoteCounts.cs
//
// PURPOSE:
// How many quotes are open, expired and withdrawn for the list's tabs, with the same search.
//
// RESPONSIBILITIES:
// - Carry the counts per state
//
// IMPORTANT NOTES:
// - Read-only data; no rule here
// =========================================================
public sealed record OperationsQuoteCounts(int Open, int Expired, int Withdrawn)
{
    public int All => Open + Expired + Withdrawn;
}
