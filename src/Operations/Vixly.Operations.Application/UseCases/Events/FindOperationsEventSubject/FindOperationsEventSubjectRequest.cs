namespace Vixly.Operations.Application.UseCases.Events.FindOperationsEventSubject;

// =========================================================
// FILE: FindOperationsEventSubjectRequest.cs
//
// PURPOSE:
// Find what a number typed in the events workspace is (Slice 6C): a parcel, a shipment, a bundle or a dispatch.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class FindOperationsEventSubjectRequest
{
    public string? Query { get; init; }
}
