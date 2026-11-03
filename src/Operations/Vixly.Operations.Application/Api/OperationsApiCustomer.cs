namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsApiCustomer.cs
//
// PURPOSE:
// A customer as the company API shows it (Slice 10).
//
// RESPONSIBILITIES:
// - Carry what the company API and its webhooks show
//
// IMPORTANT NOTES:
// - Ids, codes and figures only: never a password, a protected value or staff notes
// - A field is only ever added, never renamed or removed (receivers depend on it)
// =========================================================
public sealed record OperationsApiCustomer(
    Guid Id,
    string? Code,
    string FirstName,
    string LastName,
    string? CompanyName,
    string Email,
    string Phone,
    string Language,
    string Status,
    DateTimeOffset CreatedUtc);
