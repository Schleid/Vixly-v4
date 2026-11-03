namespace Vixly.Operations.Application.UseCases.Customers.GetMyOperationsNotices;

// =========================================================
// FILE: GetMyOperationsNoticesRequest.cs
//
// PURPOSE:
// How the signed-in customer gets the company’s notices, and the channels the company offers (Slice 9).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetMyOperationsNoticesRequest
{
    public string? SessionToken { get; init; }
}
