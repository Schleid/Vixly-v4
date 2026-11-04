namespace Vixly.Operations.Application.Dashboard;

// =========================================================
// FILE: OperationsDashboardCounter.cs
//
// PURPOSE:
// One counter of the staff home page (Slice 11; owner, 2026-10-22: counters by role that open their lists).
//
// RESPONSIBILITIES:
// - Carry its key, the list it opens and its count
//
// IMPORTANT NOTES:
// - Shown only to whoever holds the right of that list
// =========================================================
public sealed record OperationsDashboardCounter(string Key, string Link, int Count);
