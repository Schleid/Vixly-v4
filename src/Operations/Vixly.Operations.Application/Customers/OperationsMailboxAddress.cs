namespace Vixly.Operations.Application.Customers;

// =========================================================
// FILE: OperationsMailboxAddress.cs
//
// PURPOSE:
// A customer's mailbox address at one receiving warehouse (Slice 2, D-C2).
//
// RESPONSIBILITIES:
// - Carry the lines a merchant writes on the parcel
//
// IMPORTANT NOTES:
// - The first line names the customer and their code
// =========================================================
public sealed record OperationsMailboxAddress(Guid WarehouseId, string WarehouseName, string Addressee, string Line1, string? Line2, string City, string? Region, string? PostalCode, string CountryCode);
