using Vixly.Operations.Application.Receiving;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsIncomingQuery.cs
//
// PURPOSE:
// The incoming packages filter as typed in the address (Slice 4D).
//
// RESPONSIBILITIES:
// - Kept for the page's links and the export
//
// IMPORTANT NOTES:
// - Read by OperationsIncomingEndpoints
// =========================================================
public sealed record OperationsIncomingQuery(Guid? WarehouseId, OperationsIncomingView View, DateOnly? From, DateOnly? To, string? Carrier, string? Search, int Page);
