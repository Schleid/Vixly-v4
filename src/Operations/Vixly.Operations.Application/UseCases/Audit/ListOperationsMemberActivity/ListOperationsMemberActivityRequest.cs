namespace Vixly.Operations.Application.UseCases.Audit.ListOperationsMemberActivity;

// =========================================================
// FILE: ListOperationsMemberActivityRequest.cs
//
// PURPOSE:
// Member activity (owner, 2026-10-21): the company's sign-ins and sign-outs, with the device, the browser and the country, newest first.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsMemberActivityRequest
{
    public Guid? UserId { get; init; }

    public int Page { get; init; } = 1;
}
