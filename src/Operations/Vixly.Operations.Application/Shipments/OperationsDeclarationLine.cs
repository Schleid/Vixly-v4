namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsDeclarationLine.cs
//
// PURPOSE:
// One parcel on the page the customer signs (Slice 5D, D-8).
//
// RESPONSIBILITIES:
// - Carry the parcel's number, what the customer says is in it, its declared value and its weight as shown
//
// IMPORTANT NOTES:
// - Part of OperationsCustomerDeclaration
// =========================================================
public sealed record OperationsDeclarationLine(string Number, string Contents, decimal Value, string Weight);
